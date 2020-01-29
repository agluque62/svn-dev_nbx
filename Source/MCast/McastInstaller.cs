using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace U5ki.Mcast
{
	[RunInstaller(true)]
	public partial class McastInstaller : Installer
	{
		public McastInstaller()
		{
			InitializeComponent();
		}
	}
}