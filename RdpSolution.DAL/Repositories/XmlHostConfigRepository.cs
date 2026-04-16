using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using RdpSolution.DAL.Interfaces;
using RdpSolution.DAL.Models;

namespace RdpSolution.DAL.Repositories
{
    /// <summary>
    /// Persists <see cref="RemoteHostConfig"/> objects as XML elements inside a single file.
    ///
    /// File format example:
    /// <code>
    /// &lt;RemoteHosts&gt;
    ///   &lt;Host id="..."&gt;
    ///     &lt;Name&gt;My Server&lt;/Name&gt;
    ///     &lt;Hostname&gt;server.example.com&lt;/Hostname&gt;
    ///     &lt;Port&gt;3389&lt;/Port&gt;
    ///     ...
    ///   &lt;/Host&gt;
    /// &lt;/RemoteHosts&gt;
    /// </code>
    /// </summary>
    public class XmlHostConfigRepository : IHostConfigRepository
    {
        private readonly string _filePath;
        private List<RemoteHostConfig> _cache;

        public XmlHostConfigRepository(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            _filePath = filePath;
        }

        // ------------------------------------------------------------------ load / save

        private List<RemoteHostConfig> LoadFromDisk()
        {
            if (!File.Exists(_filePath))
                return new List<RemoteHostConfig>();

            var doc  = XDocument.Load(_filePath);
            var list = new List<RemoteHostConfig>();

            foreach (var el in doc.Root.Elements("Host"))
            {
                list.Add(new RemoteHostConfig
                {
                    Id             = (string)el.Attribute("id") ?? Guid.NewGuid().ToString(),
                    Name           = ReadString(el, "Name"),
                    Hostname       = ReadString(el, "Hostname"),
                    Port           = ReadInt(el,    "Port",       3389),
                    Username       = ReadString(el, "Username"),
                    Domain         = ReadString(el, "Domain"),
                    Width          = ReadInt(el,    "Width",      1024),
                    Height         = ReadInt(el,    "Height",     768),
                    FullScreen     = ReadBool(el,   "FullScreen", false),
                    AttachDrives   = ReadBool(el,   "AttachDrives",   false),
                    AttachPrinters = ReadBool(el,   "AttachPrinters", false),
                    ColorDepth     = ReadInt(el,    "ColorDepth", 32),
                    Notes          = ReadString(el, "Notes"),
                    Password       = DecryptPassword(ReadString(el, "PasswordEncrypted"))
                });
            }

            return list;
        }

        public void Save()
        {
            var list = EnsureCache();

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("RemoteHosts",
                    list.Select(h => new XElement("Host",
                        new XAttribute("id",  h.Id),
                        new XElement("Name",           h.Name           ?? string.Empty),
                        new XElement("Hostname",       h.Hostname       ?? string.Empty),
                        new XElement("Port",           h.Port),
                        new XElement("Username",       h.Username       ?? string.Empty),
                        new XElement("Domain",         h.Domain         ?? string.Empty),
                        new XElement("Width",          h.Width),
                        new XElement("Height",         h.Height),
                        new XElement("FullScreen",     h.FullScreen),
                        new XElement("AttachDrives",   h.AttachDrives),
                        new XElement("AttachPrinters", h.AttachPrinters),
                        new XElement("ColorDepth",         h.ColorDepth),
                        new XElement("Notes",              h.Notes              ?? string.Empty),
                        new XElement("PasswordEncrypted",  EncryptPassword(h.Password ?? string.Empty))
                    ))
                )
            );

            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            doc.Save(_filePath);
        }

        // ------------------------------------------------------------------ CRUD

        public IList<RemoteHostConfig> GetAll()
        {
            return EnsureCache().ToList();
        }

        public RemoteHostConfig GetById(string id)
        {
            return EnsureCache().FirstOrDefault(h => h.Id == id);
        }

        public void Add(RemoteHostConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (string.IsNullOrEmpty(config.Id))
                config.Id = Guid.NewGuid().ToString();

            EnsureCache().Add(config);
        }

        public void Update(RemoteHostConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");

            var cache = EnsureCache();
            var idx   = cache.FindIndex(h => h.Id == config.Id);
            if (idx < 0)
                throw new InvalidOperationException("Host not found: " + config.Id);

            cache[idx] = config;
        }

        public void Delete(string id)
        {
            var cache = EnsureCache();
            var item  = cache.FirstOrDefault(h => h.Id == id);
            if (item != null)
                cache.Remove(item);
        }

        // ------------------------------------------------------------------ helpers

        private List<RemoteHostConfig> EnsureCache()
        {
            if (_cache == null)
                _cache = LoadFromDisk();
            return _cache;
        }

        private static string ReadString(XElement parent, string name)
        {
            var el = parent.Element(name);
            return el != null ? el.Value : string.Empty;
        }

        private static int ReadInt(XElement parent, string name, int defaultValue)
        {
            var el = parent.Element(name);
            if (el == null) return defaultValue;
            int v;
            return int.TryParse(el.Value, out v) ? v : defaultValue;
        }

        private static bool ReadBool(XElement parent, string name, bool defaultValue)
        {
            var el = parent.Element(name);
            if (el == null) return defaultValue;
            bool v;
            return bool.TryParse(el.Value, out v) ? v : defaultValue;
        }

        // ------------------------------------------------------------------ DPAPI password helpers

        private static string EncryptPassword(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            try
            {
                byte[] cipher = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipher);
            }
            catch { return string.Empty; }
        }

        private static string DecryptPassword(string cipherBase64)
        {
            if (string.IsNullOrEmpty(cipherBase64)) return string.Empty;
            try
            {
                byte[] plain = ProtectedData.Unprotect(
                    Convert.FromBase64String(cipherBase64), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch { return string.Empty; }
        }
    }
}
