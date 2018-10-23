using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LocalChat__udp_
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		const int LOCALPORT = 4000;
		const int REMOTEPORT = 4000;
		string UserName;
		public bool isConnected { get; set; } = false;
		Socket listeningSocket;
		EndPoint remotePoint;
		BindingList<string> chatMembersIP;

		public MainWindow()
		{
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			
			this.Closing += MainWindow_Closing;
			this.Loaded += MainWindow_Loaded;
			this.KeyDown += MainWindow_KeyDown;
			textboxUsername.KeyDown += TextboxUsername_KeyDown;

			chatMembersIP = new BindingList<string>();
		}

		private void TextboxUsername_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				btnStartChat.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
		}

		private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if(isConnected)
			{
				if(e.Key == Key.Enter)
					btnSend.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			}
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Task.Run(() => ReceiveMessages());

			btnContextMenu.ItemsSource = chatMembersIP;

			textboxUsername.Focus();
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ExitChat();
		}

		private bool initialConnection()
		{
			try
			{
				listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				listeningSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

				IPEndPoint localIp = new IPEndPoint(IPAddress.Any, LOCALPORT);
				listeningSocket.Bind(localIp);

				remotePoint = new IPEndPoint(IPAddress.Broadcast, REMOTEPORT);

				sendMessage(UserName + " entered the chat.");

				isConnected = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				CloseConnection();
				return false;
			}

			return true;
		}

		private void ReceiveMessages()
		{
			try
			{
				EndPoint remoteIp = new IPEndPoint(IPAddress.Any, REMOTEPORT);

				while (true)
				{
					if (!isConnected)
					{
						Thread.Sleep(500);
						continue;
					}

					int bytes = 0;
					byte[] data = new byte[1024];
					string message = "";
					do
					{
						bytes = listeningSocket.ReceiveFrom(data, ref remoteIp);
						message += Encoding.Unicode.GetString(data, 0, bytes);
					} while (listeningSocket?.Available > 0);

					this.Dispatcher.Invoke(() =>
					{
						string time = DateTime.Now.ToShortTimeString();
						textboxChat.Text = textboxChat.Text + $"[{time}] {message}\r\n";
						textboxChat.ScrollToEnd();

						string memberIP = (remoteIp as IPEndPoint).Address.ToString();
						if (chatMembersIP.FirstOrDefault(m => m == memberIP) == null)
						{
							chatMembersIP.Add(memberIP);
							btnOnline.Content = chatMembersIP.Count;
						}
					});
				}
			}
			catch (ObjectDisposedException ex) { ReceiveMessages(); } 
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				ReceiveMessages();
			}
		}

		private void btnStartChat_Click(object sender, RoutedEventArgs e)
		{
			if (!isConnected)
			{
				UserName = textboxUsername.Text;
				if (String.IsNullOrWhiteSpace(UserName))
					return;

				initialConnection();
				changeUIelementsState();
				textboxMessage.Focus();
			}
			else
				ExitChat();

		}

		private void changeUIelementsState()
		{
			btnSend.IsEnabled = isConnected ? true : false;
			btnStartChat.Content = isConnected ? "End chat" : "Start chat";
		}

		private void ExitChat()
		{
			if (isConnected)
			{
				sendMessage(UserName + " left the chat.");
				CloseConnection();
			}
		}

		private void CloseConnection()
		{
			listeningSocket?.Shutdown(SocketShutdown.Both);
			listeningSocket?.Close();
			isConnected = false;
			changeUIelementsState();
		}

		private void btnSend_Click(object sender, RoutedEventArgs e)
		{
			string message = textboxMessage.Text;
			if (String.IsNullOrWhiteSpace(message))
				return;

			try
			{
				sendMessage(UserName + ": " + message);
				textboxMessage.Clear();
				textboxMessage.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				CloseConnection();
			}
		}

		private void sendMessage(string message)
		{
			byte[] data = Encoding.Unicode.GetBytes(message);
			listeningSocket?.SendTo(data, remotePoint);
		}

		private void textboxMessage_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			// TODO popup
			//TextBlock msg = (msgPopup.Child as TextBlock);
			//msg.Text = $"{textboxMessage.Text.Length} characters left";
			//msgPopup.IsOpen = true;
		}

		// Useless method because of IPAddress.Any
		//public IPAddress GetLocalIPAddress()
		//{
		//	IPAddress ip = null;
		//	var host = Dns.GetHostEntry(Dns.GetHostName());
		//	foreach (var item in host.AddressList)
		//	{
		//		if (item.AddressFamily == AddressFamily.InterNetwork)
		//		{
		//			ip = item;
		//		}
		//	}
		//	return ip;
		//}

		private void btnOnline_Click(object sender, RoutedEventArgs e)
		{
			btnOnline.ContextMenu.IsOpen = true;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			// TODO check is member connected
			//	MessageBox.Show((sender as MenuItem).DataContext.ToString());
			MessageBox.Show(PingHost((sender as MenuItem).DataContext.ToString(), REMOTEPORT).ToString());
		}
		public bool PingHost(string hostUri, int portNumber)
		{
			Socket s = new Socket(AddressFamily.InterNetwork,
			   SocketType.Stream,
			   ProtocolType.Tcp);
			

			try
			{
				s.Connect(hostUri, portNumber);
				Console.WriteLine("Port open");
			}
			catch (Exception)
			{
				Console.WriteLine("Port closed");
				return false;
			}
			return true;
		}
	}
}
