namespace UyiCore.Save
{
    /// <summary>
    /// Cấu hình toàn cục của <see cref="SaveSystem"/>. Gọi <see cref="SaveSystem.Configure"/>
    /// 1 lần ở Bootstrap (hoặc bỏ qua để dùng default).
    /// </summary>
    public struct SaveOptions
    {
        /// <summary>Bật XOR + Base64 wrap để chống user mở file sửa số. KHÔNG phải security.</summary>
        public bool Obfuscate;

        /// <summary>Key XOR khi Obfuscate = true. Đổi khi build release.</summary>
        public string ObfuscationKey;

        /// <summary>Đuôi file save (vd ".json" hay ".sav"). Default ".json".</summary>
        public string FileExtension;

        /// <summary>Thư mục con trong persistentDataPath. Rỗng = root. Default "Saves".</summary>
        public string Subdirectory;

        /// <summary>Tên file settings (không theo slot). Default "settings".</summary>
        public string SettingsFileName;

        /// <summary>Tên file auto-save. Default "save_auto".</summary>
        public string AutoSaveFileName;

        /// <summary>Prefix file cho slot thường (vd "save_" → save_0, save_1...). Default "save_".</summary>
        public string SlotFilePrefix;

        public static SaveOptions Default => new SaveOptions
        {
            Obfuscate = false,
            ObfuscationKey = "mycore-default-key",
            FileExtension = ".json",
            Subdirectory = "Saves",
            SettingsFileName = "settings",
            AutoSaveFileName = "save_auto",
            SlotFilePrefix = "save_",
        };
    }
}
