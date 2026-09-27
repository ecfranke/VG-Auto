# Third-party notices

VG Auto is proprietary software of V. G. Global Solution Canada Inc. (see [LICENSE](LICENSE)). It uses the open source components listed below. Each component is licensed under its own terms, which are not changed by the VG Auto license. The full license texts are included in each package (NuGet package folder or `node_modules/<package>`) and at the linked project pages.

## LGPL components

The following components are licensed under the GNU Lesser General Public License. VG Auto uses them unmodified, as separate libraries (DLLs / shared libraries) that are loaded at run time. They can be replaced by the user with compatible versions, and their source code is available from the project pages:

- NHibernate 5.4.9 (LGPL-2.1-only) - https://nhibernate.info/
- NHibernate.Driver.MySqlConnector 2.0.5 (LGPL-2.1-only) - http://nhibernate.info/
- Remotion.Linq.EagerFetching 2.2.0 (LGPL-2.1) - https://github.com/re-motion/Relinq-EagerFetching/
- @img/sharp-libvips-linux-x64 1.3.3 (LGPL-3.0-or-later, libvips binary used by the `sharp` image library in Next.js) - https://github.com/lovell/sharp-libvips
- @img/sharp-libvips-linuxmusl-x64 1.3.3 (LGPL-3.0-or-later, libvips binary used by the `sharp` image library in Next.js) - https://github.com/lovell/sharp-libvips

## Other notices

- Font Awesome Free icons: icons under CC BY 4.0, code under MIT, (c) Fonticons, Inc. - https://fontawesome.com/license/free
- caniuse-lite data: CC BY 4.0, (c) Alexis Deveria - https://github.com/browserslist/caniuse-lite
- Tailwind CSS (bundled stylesheet): MIT, (c) Tailwind Labs, Inc.
- Runtime software that is installed separately and not distributed with VG Auto: .NET runtime, Node.js, Chrome/Chromium (used for PDF generation), PostgreSQL or MySQL, nginx, pm2.

## Backend (NuGet, 151 packages)

| Package | Version | License | Project |
|---|---|---|---|
| Antlr3.Runtime | 3.5.1 | BSD-3-Clause | https://github.com/antlr/antlrcs |
| BCrypt.Net-Next | 4.0.3 | MIT |  |
| BouncyCastle.Cryptography | 2.7.0 | MIT | https://www.bouncycastle.org/stable/nuget/csharp/website |
| Dapper | 2.1.66 | Apache-2.0 | https://github.com/DapperLib/Dapper |
| dbup-core | 6.0.15 | MIT | https://dbup.github.io/ |
| dbup-mysql | 6.1.0 | MIT | https://dbup.github.io/ |
| dbup-postgresql | 6.1.5 | MIT | https://dbup.github.io/ |
| FluentNHibernate | 3.4.0 | BSD-3-Clause | https://github.com/nhibernate/fluent-nhibernate |
| Iesi.Collections | 4.0.4 | Public domain | https://github.com/nhibernate/iesi.collections |
| MailKit | 4.18.0 | MIT | http://www.mimekit.net/ |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.2 | MIT | https://asp.net/ |
| Microsoft.Bcl.Cryptography | 10.0.0 | MIT | https://dot.net/ |
| Microsoft.Extensions.ApiDescription.Server | 6.0.5 | MIT | https://asp.net/ |
| Microsoft.Extensions.Configuration | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Configuration.Binder | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Configuration.CommandLine | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Configuration.FileExtensions | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Configuration.Json | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.DependencyInjection | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Diagnostics | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Diagnostics.Abstractions | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.FileProviders.Abstractions | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.FileProviders.Physical | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.FileSystemGlobbing | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Hosting | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Hosting.Abstractions | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Hosting.Systemd | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Hosting.WindowsServices | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Http | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Logging | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Logging.Abstractions | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Logging.Configuration | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Logging.Console | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Logging.Debug | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Logging.EventLog | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Logging.EventSource | 9.0.20 | MIT | https://dot.net/ |
| Microsoft.Extensions.Options | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Options.ConfigurationExtensions | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.Extensions.Primitives | 9.0.2 | MIT | https://dot.net/ |
| Microsoft.IdentityModel.Abstractions | 8.4.0 | MIT | https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet |
| Microsoft.IdentityModel.JsonWebTokens | 8.4.0 | MIT | https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet |
| Microsoft.IdentityModel.Logging | 8.4.0 | MIT | https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet |
| Microsoft.IdentityModel.Protocols | 8.4.0 | MIT | https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.4.0 | MIT | https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet |
| Microsoft.IdentityModel.Tokens | 8.4.0 | MIT | https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet |
| Microsoft.NETCore.Platforms | 1.1.1 | Microsoft .NET Library License | https://dot.net/ |
| Microsoft.NETCore.Targets | 1.1.3 | Microsoft .NET Library License | https://dot.net/ |
| Microsoft.OpenApi | 1.6.22 | MIT | https://github.com/Microsoft/OpenAPI.NET |
| Microsoft.Win32.Primitives | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| Microsoft.Win32.SystemEvents | 6.0.0 | MIT | https://dot.net/ |
| MimeKit | 4.18.0 | MIT | https://www.mimekit.net/ |
| MySqlConnector | 2.6.2 | MIT | https://mysqlconnector.net/ |
| NETStandard.Library | 1.6.1 | Microsoft .NET Library License | https://dot.net/ |
| Newtonsoft.Json | 13.0.3 | MIT | https://www.newtonsoft.com/json |
| NHibernate | 5.4.9 | LGPL-2.1-only | https://nhibernate.info/ |
| NHibernate.Driver.MySqlConnector | 2.0.5 | LGPL-2.1-only | http://nhibernate.info/ |
| Npgsql | 9.0.4 | PostgreSQL | https://github.com/npgsql/npgsql |
| PuppeteerSharp | 20.1.0 | MIT | https://github.com/hardkoded/puppeteer-sharp |
| Remotion.Linq | 2.2.0 | Apache-2.0 | http://relinq.codeplex.com |
| Remotion.Linq.EagerFetching | 2.2.0 | LGPL-2.1 | https://github.com/re-motion/Relinq-EagerFetching/ |
| runtime.debian.8-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.fedora.23-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.fedora.24-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.native.System | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| runtime.native.System.IO.Compression | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| runtime.native.System.Net.Http | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| runtime.native.System.Security.Cryptography.Apple | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.opensuse.13.2-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.opensuse.42.1-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.osx.10.10-x64.runtime.native.System.Security.Cryptography.Apple | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| runtime.osx.10.10-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.rhel.7-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.ubuntu.14.04-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.ubuntu.16.04-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| runtime.ubuntu.16.10-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.2 | Microsoft .NET Library License | https://dot.net/ |
| Swashbuckle.AspNetCore | 7.2.0 | MIT | https://github.com/domaindrivendev/Swashbuckle.AspNetCore |
| Swashbuckle.AspNetCore.Swagger | 7.2.0 | MIT | https://github.com/domaindrivendev/Swashbuckle.AspNetCore |
| Swashbuckle.AspNetCore.SwaggerGen | 7.2.0 | MIT | https://github.com/domaindrivendev/Swashbuckle.AspNetCore |
| Swashbuckle.AspNetCore.SwaggerUI | 7.2.0 | MIT | https://github.com/domaindrivendev/Swashbuckle.AspNetCore |
| System.AppContext | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Buffers | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Collections | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Collections.Concurrent | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Configuration.ConfigurationManager | 6.0.0 | MIT | https://dot.net/ |
| System.Console | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Diagnostics.Debug | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Diagnostics.DiagnosticSource | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Diagnostics.EventLog | 9.0.20 | MIT | https://dot.net/ |
| System.Diagnostics.Tools | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Diagnostics.Tracing | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Drawing.Common | 6.0.0 | MIT | https://dot.net/ |
| System.Formats.Asn1 | 10.0.0 | MIT | https://dot.net/ |
| System.Globalization | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Globalization.Calendars | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Globalization.Extensions | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.IdentityModel.Tokens.Jwt | 8.4.0 | MIT | https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet |
| System.IO | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.IO.Compression | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.IO.Compression.ZipFile | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.IO.FileSystem | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.IO.FileSystem.Primitives | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Linq | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Linq.Expressions | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Linq.Queryable | 4.0.1 | Microsoft .NET Library License | https://dot.net/ |
| System.Net.Http | 4.3.4 | Microsoft .NET Library License | https://dot.net/ |
| System.Net.Primitives | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Net.Sockets | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.ObjectModel | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Reflection | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Reflection.Emit | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Reflection.Emit.ILGeneration | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Reflection.Emit.Lightweight | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Reflection.Extensions | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Reflection.Primitives | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Reflection.TypeExtensions | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Resources.ResourceManager | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime | 4.3.1 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime.Extensions | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime.Handles | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime.InteropServices | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime.InteropServices.RuntimeInformation | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime.Numerics | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime.Serialization.Formatters | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Runtime.Serialization.Primitives | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.AccessControl | 6.0.0 | MIT | https://dot.net/ |
| System.Security.Cryptography.Algorithms | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.Cryptography.Cng | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.Cryptography.Csp | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.Cryptography.Encoding | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.Cryptography.OpenSsl | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.Cryptography.Pkcs | 10.0.0 | MIT | https://dot.net/ |
| System.Security.Cryptography.Primitives | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.Cryptography.ProtectedData | 6.0.0 | MIT | https://dot.net/ |
| System.Security.Cryptography.X509Certificates | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Security.Permissions | 6.0.0 | MIT | https://dot.net/ |
| System.ServiceProcess.ServiceController | 9.0.20 | MIT | https://dot.net/ |
| System.Text.Encoding | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Text.Encoding.Extensions | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Text.RegularExpressions | 4.3.1 | Microsoft .NET Library License | https://dot.net/ |
| System.Threading | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Threading.Tasks | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Threading.Tasks.Extensions | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Threading.Timer | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Windows.Extensions | 6.0.0 | MIT | https://dot.net/ |
| System.Xml.ReaderWriter | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |
| System.Xml.XDocument | 4.3.0 | Microsoft .NET Library License | https://dot.net/ |

## Web app (npm production dependencies, 63 packages)

| Package | Version | License | Repository |
|---|---|---|---|
| @floating-ui/core | 1.6.9 | MIT | https://github.com/floating-ui/floating-ui |
| @floating-ui/dom | 1.6.13 | MIT | https://github.com/floating-ui/floating-ui |
| @floating-ui/react-dom | 2.1.2 | MIT | https://github.com/floating-ui/floating-ui |
| @floating-ui/react | 0.26.28 | MIT | https://github.com/floating-ui/floating-ui |
| @floating-ui/utils | 0.2.9 | MIT | https://github.com/floating-ui/floating-ui |
| @fortawesome/fontawesome-common-types | 6.7.2 | MIT | https://github.com/FortAwesome/Font-Awesome |
| @fortawesome/fontawesome-svg-core | 6.7.2 | MIT | https://github.com/FortAwesome/Font-Awesome |
| @fortawesome/free-brands-svg-icons | 6.7.2 | (CC-BY-4.0 AND MIT) | https://github.com/FortAwesome/Font-Awesome |
| @fortawesome/free-regular-svg-icons | 6.7.2 | (CC-BY-4.0 AND MIT) | https://github.com/FortAwesome/Font-Awesome |
| @fortawesome/free-solid-svg-icons | 6.7.2 | (CC-BY-4.0 AND MIT) | https://github.com/FortAwesome/Font-Awesome |
| @fortawesome/react-fontawesome | 0.2.2 | MIT | https://github.com/FortAwesome/react-fontawesome |
| @headlessui/react | 2.2.0 | MIT | https://github.com/tailwindlabs/headlessui |
| @heroicons/react | 2.2.0 | MIT | https://github.com/tailwindlabs/heroicons |
| @img/colour | 1.1.0 | MIT | https://github.com/lovell/colour |
| @img/sharp-libvips-linux-x64 | 1.3.3 | LGPL-3.0-or-later | https://github.com/lovell/sharp-libvips |
| @img/sharp-libvips-linuxmusl-x64 | 1.3.3 | LGPL-3.0-or-later | https://github.com/lovell/sharp-libvips |
| @img/sharp-linux-x64 | 0.35.4 | Apache-2.0 | https://github.com/lovell/sharp |
| @img/sharp-linuxmusl-x64 | 0.35.4 | Apache-2.0 | https://github.com/lovell/sharp |
| @next/env | 15.5.26 | MIT | https://github.com/vercel/next.js |
| @next/swc-linux-x64-gnu | 15.5.26 | MIT | https://github.com/vercel/next.js |
| @next/swc-linux-x64-musl | 15.5.26 | MIT | https://github.com/vercel/next.js |
| @react-aria/focus | 3.19.1 | Apache-2.0 | https://github.com/adobe/react-spectrum |
| @react-aria/interactions | 3.23.0 | Apache-2.0 | https://github.com/adobe/react-spectrum |
| @react-aria/ssr | 3.9.7 | Apache-2.0 | https://github.com/adobe/react-spectrum |
| @react-aria/utils | 3.27.0 | Apache-2.0 | https://github.com/adobe/react-spectrum |
| @react-stately/utils | 3.10.5 | Apache-2.0 | https://github.com/adobe/react-spectrum |
| @react-types/shared | 3.27.0 | Apache-2.0 | https://github.com/adobe/react-spectrum |
| @swc/helpers | 0.5.15 | Apache-2.0 | https://github.com/swc-project/swc |
| @tanstack/react-virtual | 3.12.0 | MIT | https://github.com/TanStack/virtual |
| @tanstack/virtual-core | 3.12.0 | MIT | https://github.com/TanStack/virtual |
| caniuse-lite | 1.0.30001697 | CC-BY-4.0 | https://github.com/browserslist/caniuse-lite |
| car-makes-icons | 1.0.0 | MIT | https://github.com/dangnelson/car-makes-icons |
| client-only | 0.0.1 | MIT |  |
| clsx | 2.1.1 | MIT | https://github.com/lukeed/clsx |
| cookie | 1.0.2 | MIT | https://github.com/jshttp/cookie |
| cookies-next | 5.1.0 | MIT | https://github.com/andreizanik/cookies-next |
| countries-and-timezones | 3.7.2 | MIT | https://github.com/manuelmhtr/countries-and-timezones |
| detect-libc | 2.1.2 | Apache-2.0 | https://github.com/lovell/detect-libc |
| framer-motion | 12.3.1 | MIT | https://github.com/motiondivision/motion |
| jose | 5.9.6 | MIT | https://github.com/panva/jose |
| js-tokens | 4.0.0 | MIT | https://github.com/lydell/js-tokens |
| jwt-decode | 4.0.0 | MIT | https://github.com/auth0/jwt-decode |
| loose-envify | 1.4.0 | MIT | https://github.com/zertosh/loose-envify |
| moment | 2.30.1 | MIT | https://github.com/moment/moment |
| motion-dom | 12.0.0 | MIT | https://github.com/motiondivision/motion |
| motion-utils | 12.0.0 | MIT | https://github.com/motiondivision/motion |
| nanoid | 3.3.19 | MIT | https://github.com/ai/nanoid |
| next | 15.5.26 | MIT | https://github.com/vercel/next.js |
| object-assign | 4.1.1 | MIT | https://github.com/sindresorhus/object-assign |
| picocolors | 1.1.1 | ISC | https://github.com/alexeyraspopov/picocolors |
| postcss | 8.4.31 | MIT | https://github.com/postcss/postcss |
| prop-types | 15.8.1 | MIT | https://github.com/facebook/prop-types |
| react-dom | 19.0.0 | MIT | https://github.com/facebook/react |
| react-is | 16.13.1 | MIT | https://github.com/facebook/react |
| react | 19.0.0 | MIT | https://github.com/facebook/react |
| scheduler | 0.25.0 | MIT | https://github.com/facebook/react |
| semver | 7.8.5 | ISC | https://github.com/npm/node-semver |
| sharp | 0.35.4 | Apache-2.0 | https://github.com/lovell/sharp |
| source-map-js | 1.2.1 | BSD-3-Clause | https://github.com/7rulnik/source-map-js |
| styled-jsx | 5.1.6 | MIT | https://github.com/vercel/styled-jsx |
| tabbable | 6.2.0 | MIT | https://github.com/focus-trap/tabbable |
| tslib | 2.8.1 | 0BSD | https://github.com/Microsoft/tslib |
| uuid | 11.1.1 | MIT | https://github.com/uuidjs/uuid |

Generated from `dotnet list package --include-transitive` and `license-checker-rseidelsohn --production`. Regenerate it when dependencies change.
