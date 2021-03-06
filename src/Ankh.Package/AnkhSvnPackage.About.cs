// $Id$
//
// Copyright 2008-2009 The AnkhSVN Project
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Reflection;
using SharpSvn;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Ankh.Configuration;
using Ankh.UI;
using SharpGit;

namespace Ankh.VSPackage
{
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#100", "1.00.0.0008", IconResourceID = 400)]
    [Ankh.VSPackage.Attributes.ProvideUIVersion]
    public partial class AnkhSvnPackage : IVsInstalledProduct
    {
        Version _uiVersion, _packageVersion;
        /// <summary>
        /// Gets the UI version. Retrieved from the registry after being installed by our MSI
        /// </summary>
        /// <value>The UI version.</value>
        public Version UIVersion
        {
            get { return _uiVersion ?? (_uiVersion = GetUIVersion() ?? PackageVersion); }
        }

        /// <summary>
        /// Gets the UI version (as might be remapped by the MSI)
        /// </summary>
        /// <returns></returns>
        private Version GetUIVersion()
        {
            // We can't use our services here to help us :(
            // This code might be used from devenv.exe /setup

            IAnkhConfigurationService configService = GetService<IAnkhConfigurationService>();

            if(configService == null)
                return null;

            using (RegistryKey rk = configService.OpenVSInstanceKey("Packages\\" + typeof(AnkhSvnPackage).GUID.ToString("b")))
            {
                if(rk == null)
                    return null;

                string v = rk.GetValue(Ankh.VSPackage.Attributes.ProvideUIVersionAttribute.RemapName) as string;

                if(string.IsNullOrEmpty(v))
                    return null;

                if(v.IndexOf('[') >= 0)
                    return null; // When not using remapping we can expect "[ProductVersion]" as value

                try
                {
                    return new Version(v);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the package version. The assembly version of Ankh.Package.dll
        /// </summary>
        /// <value>The package version.</value>
        public Version PackageVersion
        {
            get { return _packageVersion ?? (_packageVersion = typeof(AnkhSvnPackage).Assembly.GetName().Version); }
        }

        #region IVsInstalledProduct Members

        public int IdBmpSplash(out uint pIdBmp)
        {
            pIdBmp = 0; // Not used by VS2005+
            return VSErr.E_NOTIMPL;
        }

        public int IdIcoLogoForAboutbox(out uint pIdIco)
        {
            pIdIco = 400;
            return VSErr.S_OK;
        }

        public int OfficialName(out string pbstrName)
        {
            if (InCommandLineMode)
            {
                // We are running in /setup. The text is cached for the about box
                pbstrName = Resources.AboutTitleNameShort;
            }
            else
            {
                // We are running with full UI. Probably used for the about box
                pbstrName = Resources.AboutTitleName;
            }
            return VSErr.S_OK;
        }

        public int ProductDetails(out string pbstrProductDetails)
        {
            StringBuilder sb = new StringBuilder();

            string svnVersion = SvnClient.VersionString;
            if (svnVersion.EndsWith("-SharpSvn"))
                svnVersion = svnVersion.Substring(0, svnVersion.Length - 9);

            string gitVersion = GitClient.VersionString;
            if (gitVersion.EndsWith("-SharpGit"))
                gitVersion = gitVersion.Substring(0, gitVersion.Length - 9);

            sb.AppendFormat(Resources.AboutDetails,
                UIVersion.ToString(),
                PackageVersion.ToString(),
                svnVersion,
                SvnClient.SharpSvnVersion,
                gitVersion,
                GitClient.SharpGitVersion);

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendFormat(Resources.AboutLinkedTo, "SharpSvn");
            foreach (SharpSvn.Implementation.SvnLibrary lib in SvnClient.SvnLibraries)
            {
                if (!lib.Optional)
                {
                    sb.AppendFormat("{0} {1}", lib.Name, lib.VersionString);
                    sb.Append(", ");
                }
            }

            sb.Length -= 2;
            sb.AppendLine();

            bool has = false;
            foreach (SharpSvn.Implementation.SvnLibrary lib in SvnClient.SvnLibraries)
            {
                if (lib.Optional)
                {
                    if (!has)
                    {
                        has = true;
                        sb.AppendFormat(Resources.AboutOptionallyLinkedTo, "SharpSvn");
                    }

                    sb.AppendFormat("{0} {1}", lib.Name, lib.VersionString);
                    sb.Append(", ");
                }
            }

            sb.Length -= 2;

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendFormat(Resources.AboutLinkedTo, "SharpGit");
            foreach (SharpGit.Implementation.GitLibrary lib in GitClient.GitLibraries)
            {
                if (!lib.Optional)
                {
                    sb.AppendFormat("{0} {1}", lib.Name, lib.VersionString);
                    sb.Append(", ");
                }
            }

            sb.Length -= 2;
            sb.AppendLine();

            has = false;
            foreach (SharpGit.Implementation.GitLibrary lib in GitClient.GitLibraries)
            {
                if (lib.Optional)
                {
                    if (!has)
                    {
                        has = true;
                        sb.AppendFormat(Resources.AboutOptionallyLinkedTo, "SharpGit");
                    }

                    sb.AppendFormat("{0} {1}", lib.Name, lib.VersionString);
                    sb.Append(", ");
                }
            }

            sb.Length -= 2;

            pbstrProductDetails = sb.ToString();

            return VSErr.S_OK;
        }

        public int ProductID(out string pbstrPID)
        {
            pbstrPID = UIVersion.ToString();

            return VSErr.S_OK;
        }

        #endregion
    }
}
