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
                                Messages.Items.Add("Rx: " + Encoding.UTF8.GetString([.. d.DataBytes]));
                            });
                        }
                        if (d.ConnectionID == 22384114)
                        {
                            Debug.WriteLine($"Encrypted message length is {d.DataBytes.Count} bytes.");
                            string originalMessage = Aes256Helper.Decrypt([.. d.DataBytes], SHA256.HashData(Encoding.UTF8.GetBytes(textBox1.Text)));
                            Messages.Invoke(delegate ()
                            {
                                Messages.Items.Add("Rx: " + originalMessage);
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
            TransmitCurrentMessage();
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                TransmitCurrentMessage();
            }
        }
        private void TransmitCurrentMessage()
        {
            try
            {
                if (textBox1.Text == "")
                {
                    CommunicationModem?.Transmit(textBox3.Text, new(textBox2.Text), "message", 22384112);
                }
                else
                {
                    byte[] encryptedMessage = Aes256Helper.Encrypt(textBox3.Text, SHA256.HashData(Encoding.UTF8.GetBytes(textBox1.Text)));
                    CommunicationModem?.Transmit(encryptedMessage, new(textBox2.Text), "message", 22384114);
                }
                Messages.Items.Add("Tx: " + textBox3.Text);
                textBox3.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to send message!" + ex);
                MessageBox.Show("Sending your message failed for some reason", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
