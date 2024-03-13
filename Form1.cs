using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Reflection;
using Microsoft.VisualBasic;

namespace Blockchain
{
    [Serializable]
    public struct Block
    {
        //definicija bloka
        public int index;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string data;
        public DateTime timestamp;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string hash;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string previousHash;
        public int diff;
        public double cumulativeDifficulty;
        public int nonce;
    }
    public static class RichTextBoxUtils
    {

        public static void AppendColoredText(this RichTextBox box, string text, Color color)
        {
            text += Environment.NewLine;
            box.BeginInvoke((MethodInvoker)delegate {
                box.SelectionStart = box.TextLength;
                box.SelectionLength = 0;
                box.SelectionColor = color;
                box.AppendText(text);
                box.SelectionColor = box.ForeColor;
            });
        }

        public static void InvokeAppendText(this RichTextBox box, string text, Color color)
        {

            if (box.IsHandleCreated)
            {
                if (box.InvokeRequired)
                {
                    box.Invoke(new Action(() => AppendColoredTextInternal(box, text, color)));
                }
                else
                {
                    AppendColoredTextInternal(box, text, color);
                }
            }

        }

        private static void AppendColoredTextInternal(RichTextBox box, string text, Color color)
        {
            text += Environment.NewLine;
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }



    public partial class Form1 : Form
    {

        private List<TcpClient> connectedClients = new List<TcpClient>();
        readonly string IP = "127.0.0.1";
        int PORT = 1234;
        readonly int MSG_SIZE = 1024;
        readonly int BlockBitSize = 128;
        readonly int KeyBitSize = 256;
        List<Block> blockchain = new List<Block>();
        int diff = 5;
        int index = 0;
        int hashOutputInterval = 100000; 
        TimeSpan intervalBetweenBlocks = TimeSpan.FromSeconds(10);
        readonly int blockCheckDiff = 10; 
        Thread miningThread;
        Thread connectionThread;
        Thread outboundConnectionThread;
        SemaphoreSlim semaphore = new SemaphoreSlim(0, 9); 
        bool stopMining = false;



        
        private byte[] ReceiveData(NetworkStream ns)
        {
            List<byte> receivedData = new List<byte>();
            byte[] buffer = new byte[MSG_SIZE];
            try
            {
                int bytesRead;
                do
                {
                    bytesRead = ns.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        receivedData.AddRange(buffer.Take(bytesRead));
                    }
                }
                while (ns.DataAvailable);

                return receivedData.ToArray();
            }
            catch (Exception ex)
            {
                labelStatus.Text = "ReceiveData Error: " + ex.Message;
                return null;
            }
        }

        private void TransmitData(NetworkStream ns, byte[] data)
        {
            try
            {
                if (ns.CanWrite)
                {
                    ns.Write(data, 0, data.Length);
                }
                else
                {
                    throw new InvalidOperationException("Cannot write to stream.");
                }
            }
            catch (Exception ex)
            {
                labelStatus.Text = "TransmitData Error: " + ex.Message;
            }
        }

        public event EventHandler<Block> NewBlockAdded;


        protected virtual void OnNewBlockAdded(Block block)
        {
            NewBlockAdded?.Invoke(this, block);
            RichTextBoxUtils.InvokeAppendText(infoMining, $"New block found! Index: {block.index}, Hash: {block.hash}, Cumulative Difficulty: {block.cumulativeDifficulty}", Color.Blue);
        }


        private void InitializeServer()
        {
            PORT = FindAvailablePort();
            var server = new TcpListener(IPAddress.Parse(IP), PORT);
            try
            {
                server.Start();
                UpdateStatusLabel($"Server started on port {PORT}");

                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var client = server.AcceptTcpClient();
                            UpdateStatusLabel($"Connected client: {client.Client.RemoteEndPoint.ToString()}");
                            Thread clientThread = new Thread(() => HandleClient(client));
                            clientThread.Start();
                        }
                        catch (Exception ex)
                        {
                            UpdateStatusLabel($"Error accepting client: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Server error: {ex.Message}");
            }
        }


        private void AssessBlockchain(List<Block> externalChain)
        {
            var currentWork = blockchain.Sum(b => Math.Pow(2, b.diff));
            var incomingWork = externalChain.Sum(b => Math.Pow(2, b.diff));

            if (incomingWork > currentWork)
            {
                if (miningCancellationTokenSource != null && !miningCancellationTokenSource.IsCancellationRequested)
                {
                    StopMining();
                }

                blockchain = new List<Block>(externalChain);
                index = blockchain.Last().index + 1;
                diff = blockchain.Last().diff;

               
                this.Invoke(new Action(() =>
                {
                    infoBlockchain.Clear();
                    foreach (var block in blockchain)
                    {
                        DisplayBlockInfo(block);
                    }
                }));

                RichTextBoxUtils.AppendColoredText(infoMining, "Blockchain updated to the new, more difficult chain.", Color.Green);

                if (!stopMining)
                {
                    StartMining();
                }
            }
            else
            {
                RichTextBoxUtils.AppendColoredText(infoMining, "Current blockchain retained. Incoming chain not longer.", Color.Yellow);
            }

            if (index % blockCheckDiff == 0)
            {
                AdjustDifficulty(blockchain.Last());
            }

            RichTextBoxUtils.AppendColoredText(infoMining, "Blockchain accepted with last block hash: " + blockchain.Last().hash, Color.Green);
        }

        private async void HandleClient(TcpClient client)
        {
            connectedClients.Add(client);

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[MSG_SIZE];

                    while (true)
                    {
                        int bytesRead = 0;
                        List<byte> receivedData = new List<byte>();

                        try
                        {
                            do
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    receivedData.AddRange(buffer.Take(bytesRead));
                                }
                            }
                            while (stream.DataAvailable);
                        }
                        catch (IOException)
                        {
                           
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            
                            break;
                        }

                        Block receivedBlock = DeserializeBlock(receivedData.ToArray());
                        bool isValid = ValidateReceivedBlock(receivedBlock);

                        if (isValid)
                        {
                            blockchain.Add(receivedBlock); 
                                                           
                            NotifyClientsNewBlock(receivedBlock); 
                        }

                        
                        byte[] blockchainData = SerializeBlockchain(blockchain);
                        await stream.WriteAsync(blockchainData, 0, blockchainData.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                 UpdateStatusLabel($"Client handling error: {ex.Message}");
            }
            finally
            {
                
                connectedClients.Remove(client);
                client.Close();
            }
        }

        private List<Block> DeserializeBlockchain(byte[] data)
        {
            
            string jsonString = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<List<Block>>(jsonString);
        }


        private bool ValidateReceivedBlock(Block block)
        {
            return block.index == blockchain.Count &&
                   block.previousHash == blockchain.Last().hash &&
                   IsValidHash(block.hash, block.diff);
        }

        private Block DeserializeBlock(byte[] data)
        {
            
            string jsonString = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<Block>(jsonString);
        }

        private byte[] SerializeBlockchain(List<Block> blockchain)
        {
            
            string jsonString = JsonSerializer.Serialize(blockchain);
            return Encoding.UTF8.GetBytes(jsonString);
        }



        private byte[] SerializeBlock(Block block)
        {
            
            string jsonString = JsonSerializer.Serialize(block);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        private async void NotifyClientsNewBlock(Block block)
        {
            byte[] blockData = SerializeBlock(block); 
            foreach (var client in connectedClients.ToList()) 
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        await stream.WriteAsync(blockData, 0, blockData.Length);
                    }
                }
                catch (Exception ex)
                {
                    
                    connectedClients.Remove(client);
                }
            }
        }



        private void UpdateStatusLabel(string message)
        {

            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    labelStatus.Text = message;
                });
            }

        }
        public Form1()
        {
            InitializeComponent();
            InitializeServer();
            NewBlockAdded += NewBlockAddedHandler;
        }

        
        static string CalculateHash(string rawData)
        {
            using (var hasher = new SHA256Managed())
            {
                byte[] hashBytes = hasher.ComputeHash(Encoding.ASCII.GetBytes(rawData));
                return string.Concat(hashBytes.Select(b => b.ToString("x2")));
            }
        }


        private void AdjustDifficulty(Block newBlock)
        {
            
            if (newBlock.index >= blockCheckDiff)
            {
                Block prevAdjustmentBlock = blockchain[newBlock.index - blockCheckDiff];
                TimeSpan expectedDuration = intervalBetweenBlocks * blockCheckDiff; 
                TimeSpan actualDuration = newBlock.timestamp - prevAdjustmentBlock.timestamp;

                if (actualDuration < expectedDuration / 2)
                {
                    RichTextBoxUtils.AppendColoredText(infoMining, "Increasing difficulty", Color.Black);
                    diff = prevAdjustmentBlock.diff + 1;
                }
                else if (actualDuration > expectedDuration * 2)
                {
                    diff = prevAdjustmentBlock.diff - 1;
                    RichTextBoxUtils.AppendColoredText(infoMining, "Decreasing difficulty", Color.Blue);
                }
                else
                {
                    diff = prevAdjustmentBlock.diff;
                    RichTextBoxUtils.AppendColoredText(infoMining, "Difficulty remains unchanged", Color.Brown);
                }
            }
            else
            {
              
                RichTextBoxUtils.AppendColoredText(infoMining, "Difficulty adjustment not required yet", Color.Orange);
            }
        }




        
        private Block PrepareNewBlock(Block newBlock)
        {
           
            if (blockchain.Count == 0)
            {
                newBlock.index = index;
                newBlock.data = "Genesis Block created by " + textBoxNodeName.Text;
                newBlock.timestamp = DateTime.Now;
                newBlock.previousHash = "0";
                newBlock.diff = diff;
                newBlock.cumulativeDifficulty = Math.Pow(2, newBlock.diff);
            }
            else
            {
                newBlock.index = index;
                newBlock.data = "Block created by " + textBoxNodeName.Text;
                newBlock.timestamp = DateTime.Now;
                newBlock.previousHash = blockchain.Last().hash;
                newBlock.diff = diff;
                newBlock.cumulativeDifficulty = blockchain.Last().cumulativeDifficulty + Math.Pow(2, newBlock.diff);//Izračun kumulativne težavnosti verig
            }
            return newBlock;
        }

       
        private async void MineBlocks(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Block newBlock = new Block();
                newBlock = PrepareNewBlock(newBlock);

                int attempts = 0;
                bool foundBlock = false;
                while (!foundBlock && !cancellationToken.IsCancellationRequested)
                {
                    newBlock = PerformProofOfWork(newBlock, out foundBlock); 

                    if (foundBlock) 
                    {
                        blockchain.Add(newBlock);
                        DisplayBlockInfo(newBlock);
                        index++;
                        if (newBlock.index % blockCheckDiff == 0)
                        {
                            AdjustDifficulty(newBlock);
                        }

                        OnNewBlockAdded(newBlock);

                        RichTextBoxUtils.InvokeAppendText(infoMining, $"New block found! Index: {newBlock.index}, Cumulative Difficulty: {newBlock.cumulativeDifficulty}", Color.Blue);
                        await SendNewBlockToClients(newBlock);

                    }

                    attempts++;
                    if (attempts % hashOutputInterval == 0)
                    {
                        RichTextBoxUtils.InvokeAppendText(infoMining, $"Mining attempts: {attempts}", Color.DarkCyan);
                    }
                }

                Thread.Sleep(5000); 
            }

            RichTextBoxUtils.InvokeAppendText(infoMining, "Mining stopped.", Color.Red);
        }



       
        private async Task SendNewBlockToClients(Block newBlock)
        {

            foreach (var client in connectedClients)
            {
                try
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] blockData = getBytes(newBlock);
                        await stream.WriteAsync(blockData, 0, blockData.Length);
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }

        private void DisplayBlockInfo(Block block)
        {
            Action<string, Color> updateBlockchainInfo = (text, color) => infoBlockchain.BeginInvoke((MethodInvoker)delegate () {
                infoBlockchain.SelectionStart = infoBlockchain.TextLength;
                infoBlockchain.SelectionLength = 0;
                infoBlockchain.SelectionColor = color;
                infoBlockchain.AppendText(text);
                infoBlockchain.SelectionColor = infoBlockchain.ForeColor;
            });

            var info = new StringBuilder();
            info.AppendLine("-------------------------------");
            updateBlockchainInfo($"Index: {block.index}\n", Color.Black);
            updateBlockchainInfo($"Data: {block.data}\n", Color.Black);
            updateBlockchainInfo($"Timestamp: {block.timestamp}\n", Color.Black);
            updateBlockchainInfo($"Hash: {block.hash}\n", Color.Black);
            updateBlockchainInfo($"Previous Hash: {block.previousHash}\n", Color.Black);
            updateBlockchainInfo($"Difficulty: {block.diff}\n", Color.Brown);
            updateBlockchainInfo($"Nonce: {block.nonce}\n", Color.Black);
        }

        
        private string ValidateNewBlock(Block block, int expectedIndex)
        {
           
            var errors = new StringBuilder();
            string inputForHash = block.index.ToString() + block.timestamp.ToString("o") + block.data + block.previousHash + block.diff.ToString() + block.nonce.ToString();
            string computedHash = CalculateHash(inputForHash);

           
            if (block.index != expectedIndex)
                errors.AppendLine($"Block index {block.index} does not match expected index {expectedIndex}.");

            if (block.hash != computedHash)
                errors.AppendLine($"Block hash {block.hash} does not match computed hash {computedHash}.");

            if (expectedIndex > 0 && block.previousHash != blockchain[expectedIndex - 1].hash)
                errors.AppendLine($"Previous hash {block.previousHash} does not match actual previous block hash {blockchain[expectedIndex - 1].hash}.");
            return "Block is valid";
        }

        
        private bool IsValidHash(string hash, int difficulty)
        {
            
            string requiredPrefix = new string('0', difficulty);
            return hash.StartsWith(requiredPrefix);
        }


      

        private async void ListenForBlocks(NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[MSG_SIZE];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(buffer, receivedData, bytesRead);
                        Block receivedBlock = DeserializeBlock(receivedData);

                        if (ValidateReceivedBlock(receivedBlock))
                        {
                            
                            blockchain.Add(receivedBlock);
                            DisplayBlockInfo(receivedBlock);
                        }
                    }
                    else
                    {
                        
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                
                UpdateStatusLabel($"Error while listening for blocks: {ex.Message}");
            }
        }

        private void InitializeNetworkConnection()
        {
            int port = FindAvailablePort();
            TcpListener server = new TcpListener(IPAddress.Parse(IP), port);

            try
            {
                server.Start();
                UpdateStatusLabel($"Listening on port: {port}");

                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            TcpClient client = server.AcceptTcpClient();


                            Task.Run(() => HandleClient(client));
                        }
                    }
                    catch (SocketException ex)
                    {
                        UpdateStatusLabel($"SocketException: {ex.Message}");
                    }
                    finally
                    {
                        server.Stop();
                    }
                });
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Network error: {ex.Message}");
                server?.Stop();
            }
        }

       
        private async Task<List<Block>> ReceiveBlockchainAsync(NetworkStream stream)
        {
            List<Block> blocks = new List<Block>();
            byte[] buffer = new byte[MSG_SIZE];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            while (bytesRead > 0)
            {
                byte[] data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);
                Block block = fromBytes(data);
                blocks.Add(block);
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            }

            return blocks;
        }


        private async Task TransmitBlockchainAsync(NetworkStream stream, List<Block> blockchain)
        {
            foreach (var block in blockchain)
            {
                byte[] blockData = getBytes(block);
                await stream.WriteAsync(blockData, 0, blockData.Length);
            }
        }


       
        private byte[] getBytes(Block block)
        {
            int size = Marshal.SizeOf(block);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(block, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }


        private Block fromBytes(byte[] arr)
        {
            Block block = new Block();

            int size = Marshal.SizeOf(block);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            block = (Block)Marshal.PtrToStructure(ptr, block.GetType());
            Marshal.FreeHGlobal(ptr);

            return block;
        }


        private int FindAvailablePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

       
        private CancellationTokenSource miningCancellationTokenSource;
        private void StartMining()
        {
            if (miningCancellationTokenSource != null)
            {
                StopMining();
            }
            miningCancellationTokenSource = new CancellationTokenSource();
            miningThread = new Thread(() => MineBlocks(miningCancellationTokenSource.Token));
            miningThread.IsBackground = true;
            miningThread.Start();
            UpdateStatusLabel("Mining started.");
        }

        private void StopMining()
        {
            if (miningCancellationTokenSource != null)
            {
                miningCancellationTokenSource.Cancel();
                miningThread.Join();
                miningCancellationTokenSource = null;
                UpdateStatusLabel("Mining stopped.");
            }
        }

        private void StartServer()
        {
            connectionThread = new Thread(InitializeNetworkConnection);
            connectionThread.IsBackground = true;
            connectionThread.Start();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {

            PORT = FindAvailablePort();
            StartServer();
            UpdateStatusLabel($"Server is listening on port: {PORT}");
        }

        private void buttonConnectPort_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxPort.Text, out int portToConnect))
            {
                Task.Run(() => ConnectToServerAsync(portToConnect));
            }
            else
            {
                UpdateStatusLabel("Invalid port number.");
            }
        }
        private async Task ConnectToServerAsync(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Parse(IP), port);

                    if (client.Connected)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            UpdateStatusLabel("Connection established to port: " + port);
                        });

                        using (var stream = client.GetStream())
                        {

                            await TransmitBlockchainAsync(stream, blockchain);


                            List<Block> receivedBlockchain = await ReceiveBlockchainAsync(stream);
                            AssessBlockchain(receivedBlockchain);
                        }
                    }
                    else
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            UpdateStatusLabel("Failed to connect to port: " + port);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    UpdateStatusLabel("ConnectToServer Error: " + ex.Message);
                });
            }
        }

        private void buttonMine_Click(object sender, EventArgs e)
        {
            StartMining();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            this.Close();
            StopMining();
            this.Close();
        }

        private void NewBlockAddedHandler(object sender, Block newBlock)
        {
            foreach (var client in connectedClients)
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] blockData = getBytes(newBlock);
                        stream.WriteAsync(blockData, 0, blockData.Length);
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }
        }

       
        private Block PerformProofOfWork(Block block, out bool foundBlock)
        {
           
            string computedHash;
            int nonce = 0;
            do
            {
                string inputForHash = block.index.ToString() + block.timestamp.ToString("o") + block.data + block.previousHash + block.diff.ToString() + nonce.ToString();
                computedHash = CalculateHash(inputForHash);
                nonce++;

                if (IsValidHash(computedHash, block.diff))
                {
                    foundBlock = true;
                    block.nonce = nonce;
                    block.hash = computedHash;


                    if (blockchain.Count == 0)
                    {

                        block.cumulativeDifficulty = Math.Pow(2, block.diff);
                    }
                    else
                    {

                        block.cumulativeDifficulty = blockchain.Last().cumulativeDifficulty + Math.Pow(2, block.diff);
                    }

                    return block;
                }
            } while (!stopMining);

            foundBlock = false;
            return block;
        }
    }
}

