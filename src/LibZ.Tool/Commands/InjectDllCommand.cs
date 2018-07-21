﻿#region License

/*
 * Copyright (c) 2013-2014, Milosz Krajewski
 * 
 * Microsoft Public License (Ms-PL)
 * This license governs use of the accompanying software. 
 * If you use the software, you accept this license. 
 * If you do not accept the license, do not use the software.
 * 
 * 1. Definitions
 * The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same 
 * meaning here as under U.S. copyright law.
 * A "contribution" is the original software, or any additions or changes to the software.
 * A "contributor" is any person that distributes its contribution under this license.
 * "Licensed patents" are a contributor's patent claims that read directly on its contribution.
 * 
 * 2. Grant of Rights
 * (A) Copyright Grant- Subject to the terms of this license, including the license conditions 
 * and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
 * royalty-free copyright license to reproduce its contribution, prepare derivative works of 
 * its contribution, and distribute its contribution or any derivative works that you create.
 * (B) Patent Grant- Subject to the terms of this license, including the license conditions and 
 * limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
 * royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, 
 * import, and/or otherwise dispose of its contribution in the software or derivative works of 
 * the contribution in the software.
 * 
 * 3. Conditions and Limitations
 * (A) No Trademark License- This license does not grant you rights to use any contributors' name, 
 * logo, or trademarks.
 * (B) If you bring a patent claim against any contributor over patents that you claim are infringed 
 * by the software, your patent license from such contributor to the software ends automatically.
 * (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, 
 * and attribution notices that are present in the software.
 * (D) If you distribute any portion of the software in source code form, you may do so only under this 
 * license by including a complete copy of this license with your distribution. If you distribute 
 * any portion of the software in compiled or object code form, you may only do so under a license 
 * that complies with this license.
 * (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express
 * warranties, guarantees or conditions. You may have additional consumer rights under your local 
 * laws which this license cannot change. To the extent permitted under your local laws, the 
 * contributors exclude the implied warranties of merchantability, fitness for a particular 
 * purpose and non-infringement.
 */

#endregion

using System.Collections.Generic;
using LibZ.Tool.Tasks;
using ManyConsole;

namespace LibZ.Tool.Commands
{
	/// <summary>Inject DLL command.</summary>
	public class InjectDllCommand: ConsoleCommand
	{
		#region fields

		private string _mainFileName;
		private string _keyFileName;
		private string _appConfigFile;
		private string _keyFilePassword;
		private readonly List<string> _include = new List<string>();
		private readonly List<string> _exclude = new List<string>();
		private bool _overwrite;
		private bool _move;

		#endregion

		#region constructor

		/// <summary>Initializes a new instance of the <see cref="InjectDllCommand"/> class.</summary>
		public InjectDllCommand()
		{
			IsCommand("inject-dll", "Injects .dll file into assembly as resource");
			HasRequiredOption("a|assembly=", "main file name (.exe or .dll)", s => _mainFileName = s);
			HasRequiredOption("i|include=", "assembly file name to include (wildcards allowed)", s => _include.Add(s));
			HasOption("e|exclude=", "assembly file name to exclude (wildcards allowed, optional)", s => _exclude.Add(s));
			HasOption("overwrite", "Overwrites existing resources", _ => _overwrite = true);
			HasOption("move", "move files (remove when added)", _ => _move = true);
			HasOption("k|key=", "key file name", s => _keyFileName = s);
			HasOption("b|bindingFile=", "App config file (xml), with assemblyBinding node", s => _appConfigFile = s);
			HasOption("p|password=", "password for password protected key file", s => _keyFilePassword = s);
		}

		#endregion

		/// <summary>Runs the command.</summary>
		/// <param name="remainingArguments">The remaining arguments.</param>
		/// <returns>Return code.</returns>
		public override int Run(string[] remainingArguments)
		{
			var task = new InjectDllTask();
			task.Execute(
				_mainFileName,
				_include.ToArray(), _exclude.ToArray(),
				_keyFileName, _keyFilePassword,
				_overwrite, _move, _appConfigFile);
			return 0;
		}
	}
}
