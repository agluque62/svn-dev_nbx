using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace U5ki.NodeBox
{
	[RunInstaller(true)]
	public partial class NodeBoxInstaller : Installer
	{
		public NodeBoxInstaller()
		{
			InitializeComponent();
		}
	}
}