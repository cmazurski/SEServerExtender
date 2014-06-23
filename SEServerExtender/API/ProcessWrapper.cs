﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Sandbox.Common.ObjectBuilders;

using SEModAPI.API;
using SEModAPI.API.Internal;
using SEModAPI.API.SaveData;
using SEModAPI.API.SaveData.Entity;

using VRage.Common.Utils;

namespace SEServerExtender.API
{
	public class ProcessWrapper
	{
		#region "Attributes"

		private ServerAssemblyWrapper m_serverWrapper;
		private SandboxGameAssemblyWrapper m_sandboxGameWrapper;
		private GameObjectManagerWrapper m_gameObjectManagerWrapper;

		private static Thread m_runServerThread;
		private static Thread m_monitorServerThread;

		private static bool m_serverRunning;
		private static string m_worldName;

		#endregion

		#region "Constructors and Initializers"

		public ProcessWrapper()
		{
			m_serverRunning = false;
		}

		public void StartGame(string worldName)
		{
			try
			{
				if (m_serverRunning)
					return;

				string basePath = Path.Combine(GameInstallationInfo.GamePath, "DedicatedServer64");
				m_serverWrapper = new ServerAssemblyWrapper(basePath);
				m_sandboxGameWrapper = new SandboxGameAssemblyWrapper(basePath);
				m_gameObjectManagerWrapper = new GameObjectManagerWrapper(basePath);

				m_worldName = worldName;

				MyFileSystem.Reset();

				string contentPath = Path.Combine(new FileInfo(MyFileSystem.ExePath).Directory.FullName, "Content");
				MyFileSystem.Init(contentPath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineersDedicated"));
				MyFileSystem.InitUserSpecific((string)null);

				m_runServerThread = new Thread(new ThreadStart(this.RunServer));
				m_runServerThread.Start();
				m_monitorServerThread = new Thread(new ThreadStart(this.MonitorServer));
				m_monitorServerThread.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		#endregion

		#region "Methods"

		private void MonitorServer()
		{
			B337879D0C82A5F9C44D51D954769590.B3531963E948FB4FA1D057C4340C61B4 mainGame = null;
			MyConfigDedicatedData config = null;

			bool isLoaded = false;
			while (!isLoaded)
			{
				Console.WriteLine("MONITOR - Waiting for server to load ...");

				Thread.Sleep(1000);

				mainGame = m_sandboxGameWrapper.GetMainGameInstance();
				if (mainGame == null)
					continue;
				config = SandboxGameAssemblyWrapper.GetServerConfig();
				if (config == null)
					continue;
				if (config.LoadWorld == null)
					continue;

				isLoaded = true;
			}

			//TODO - Find a way to determine when the server is fully loaded
			Thread.Sleep(25000);

			Console.WriteLine("MONITOR - Server has started");

			m_serverRunning = true;

			while (m_serverRunning && m_runServerThread.ThreadState != System.Threading.ThreadState.Stopped)
			{
				mainGame = m_sandboxGameWrapper.GetMainGameInstance();
				config = SandboxGameAssemblyWrapper.GetServerConfig();
				if (mainGame == null || config == null)
				{
					m_serverRunning = false;
					continue;
				}

				Thread.Sleep(2000);
			}

			m_serverRunning = false;

			Console.WriteLine("MONITOR - Server has shut down");
		}

		private void RunServer()
		{
			m_gameObjectManagerWrapper.GameThread = Thread.CurrentThread;
			m_serverRunning = m_serverWrapper.StartServer(m_worldName);
		}

		public GameObjectManagerWrapper GetGameObjectManager()
		{
			return m_gameObjectManagerWrapper;
		}

		#endregion
	}
}
