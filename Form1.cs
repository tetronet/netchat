using ModemAPI;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Netchat
{
    public partial class Form1 : Form
    {
        private IModem? CommunicationModem = null;
        private string TetronetAccessServer;
        public Form1()
        {
            InitializeComponent();
            TetronetAccessServer = File.ReadAllText("cias.txt");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists("ci_address.txt"))
            {
                CommunicationModem = new VirtualModem(TetronetAccessServer, new());
            }
            else
            {
                CommunicationModem = new VirtualModem(TetronetAccessServer, new(new(File.ReadAllText("ci_address.txt")), false, null));
            }
            CommunicationModem.Dial();
            while (!CommunicationModem.IsModemConnected) { Thread.Sleep(1); }
            label2.Text = $"Tetronet address (yours is {CommunicationModem.LocalModemAddress}):";
            CommunicationModem.AttachReceiveEventNoUnfragment(delegate (Packet d, Action k)
            {
                try
                {
                    if (d.QueryType == "message" && d.Transmitter.Equals(new(textBox2.Text)))
                    {
                        if (d.ConnectionID == 22384112)
                        {
                            Messages.Invoke(delegate ()
                            {
                                Messages.Items.Add("Received Message: " + Encoding.UTF8.GetString([..d.DataBytes]));
                            });
                        }
                        if (d.ConnectionID == 22384114)
                        {
                            byte[] iv = [.. d.DataBytes.Take(16)];
                            byte[] data = [.. d.DataBytes.Skip(16)];
                            Debug.WriteLine($"Encrypted message length is {data.Length} bytes.");
                            string originalMessage = Aes256Helper.Decrypt(data, SHA256.HashData(Encoding.UTF8.GetBytes(textBox1.Text)), iv);
                            Messages.Invoke(delegate ()
                            {
                                Messages.Items.Add("Received Message: " + originalMessage);
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to receive message!" + ex);
                }
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox1.Text == "")
                {
                    CommunicationModem?.Transmit(textBox3.Text, new(textBox2.Text), "message", 22384112);
                }
                else
                {
                    byte[] iv = new byte[16];
                    RandomNumberGenerator.Fill(iv);
                    byte[] encryptedMessage = Aes256Helper.Encrypt(textBox3.Text, SHA256.HashData(Encoding.UTF8.GetBytes(textBox1.Text)), iv);
                    byte[] encryptedMessageWithIv = new byte[16 + encryptedMessage.Length];
                    
                    iv.CopyTo(encryptedMessageWithIv, 0);
                    encryptedMessage.CopyTo(encryptedMessageWithIv, 16);
                    CommunicationModem?.Transmit(encryptedMessageWithIv, new(textBox2.Text), "message", 22384114);
                }
                Messages.Items.Add("Sent Message: " + textBox3.Text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to send message!" + ex);
                MessageBox.Show("Sending your message failed for some reason", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
