// ============================================================
// SmartGPON — Infrastructure/Helpers/MimeHelper.cs
// Vérification MIME magic bytes — sans package externe
// A2: NuGet hors ligne → implémentation manuelle des signatures
// ============================================================
namespace SmartGPON.Infrastructure.Helpers
{
    public static class MimeHelper
    {
        // Signatures magic bytes: extension → (bytes attendus, offset, MIME)
        private static readonly List<(byte[] Magic, int Offset, string[] Extensions, string Mime)> Signatures = new()
        {
            // JPEG
            (new byte[]{ 0xFF, 0xD8, 0xFF }, 0, new[]{ ".jpg", ".jpeg" }, "image/jpeg"),
            // PNG
            (new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0,
              new[]{ ".png" }, "image/png"),
            // GIF
            (new byte[]{ 0x47, 0x49, 0x46, 0x38 }, 0, new[]{ ".gif" }, "image/gif"),
            // BMP
            (new byte[]{ 0x42, 0x4D }, 0, new[]{ ".bmp" }, "image/bmp"),
            // WebP (RIFF....WEBP)
            (new byte[]{ 0x57, 0x45, 0x42, 0x50 }, 8, new[]{ ".webp" }, "image/webp"),
            // TIFF LE
            (new byte[]{ 0x49, 0x49, 0x2A, 0x00 }, 0, new[]{ ".tiff" }, "image/tiff"),
            // TIFF BE
            (new byte[]{ 0x4D, 0x4D, 0x00, 0x2A }, 0, new[]{ ".tiff" }, "image/tiff"),
            // PDF
            (new byte[]{ 0x25, 0x50, 0x44, 0x46 }, 0,
              new[]{ ".pdf" }, "application/pdf"),
            // ZIP-based formats (Office 2007+) : .docx .xlsx .pptx
            (new byte[]{ 0x50, 0x4B, 0x03, 0x04 }, 0,
              new[]{ ".docx", ".xlsx", ".pptx", ".doc", ".xls", ".ppt" },
              "application/zip"),
            // OLE2 (Office 97-2003): .doc .xls .ppt
            (new byte[]{ 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, 0,
              new[]{ ".doc", ".xls", ".ppt" }, "application/x-ole-storage"),
            // DWG R14+
            (new byte[]{ 0x41, 0x43, 0x31, 0x30 }, 0,
              new[]{ ".dwg" }, "image/vnd.dwg"),
            // SVG (check UTF8 BOM or tag — fallback: text)
            (new byte[]{ 0x3C, 0x73, 0x76, 0x67 }, 0, new[]{ ".svg" }, "image/svg+xml"),
            (new byte[]{ 0x3C, 0x3F, 0x78, 0x6D }, 0, new[]{ ".svg" }, "image/svg+xml"),
        };

        // Whitelist: ext → MIME(s) acceptés
        private static readonly Dictionary<string, string[]> AllowedMimes = new()
        {
            { ".jpg",  new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png",  new[] { "image/png" } },
            { ".gif",  new[] { "image/gif" } },
            { ".bmp",  new[] { "image/bmp" } },
            { ".webp", new[] { "image/webp" } },
            { ".tiff", new[] { "image/tiff" } },
            { ".svg",  new[] { "image/svg+xml", "text/plain" } },
            { ".pdf",  new[] { "application/pdf" } },
            { ".doc",  new[] { "application/x-ole-storage", "application/zip" } },
            { ".docx", new[] { "application/zip" } },
            { ".ppt",  new[] { "application/x-ole-storage", "application/zip" } },
            { ".pptx", new[] { "application/zip" } },
            { ".xls",  new[] { "application/x-ole-storage", "application/zip" } },
            { ".xlsx", new[] { "application/zip" } },
            { ".dwg",  new[] { "image/vnd.dwg" } },
        };

        // Retourne le MIME détecté via magic bytes. Retourne null si non reconnu.
        public static string? DetectMime(Stream stream)
        {
            const int bufLen = 16;
            var buf = new byte[bufLen];
            var saved = stream.Position;
            stream.Position = 0;
            int read = stream.Read(buf, 0, bufLen);
            stream.Position = saved;

            foreach (var (magic, offset, _, mime) in Signatures)
            {
                if (offset + magic.Length > read) continue;
                bool match = true;
                for (int i = 0; i < magic.Length; i++)
                {
                    if (buf[offset + i] != magic[i]) { match = false; break; }
                }
                if (match) return mime;
            }
            // Fallback SVG: check text/xml content
            if (read > 0) return "application/octet-stream";
            return null;
        }

        // Retourne true si le MIME détecté est compatible avec l'extension déclarée
        public static bool IsAllowed(string ext, string detectedMime)
        {
            ext = ext.ToLower();
            if (!AllowedMimes.TryGetValue(ext, out var allowed)) return false;
            return allowed.Any(m => detectedMime.StartsWith(m, StringComparison.OrdinalIgnoreCase));
        }

        // Retourne le MIME "canonique" à stocker en DB pour une extension donnée
        public static string GetCanonicalMime(string ext)
        {
            return ext.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".gif"  => "image/gif",
                ".bmp"  => "image/bmp",
                ".webp" => "image/webp",
                ".tiff" => "image/tiff",
                ".svg"  => "image/svg+xml",
                ".pdf"  => "application/pdf",
                ".doc"  => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".ppt"  => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".xls"  => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".dwg"  => "image/vnd.dwg",
                _       => "application/octet-stream"
            };
        }
    }
}
