using System.Runtime.InteropServices;
using System.IO;
using System;

public static class TinyFileDialogs {
    public enum DialogType { OK, OKCANCEL, YESNO };
    public enum IconType { INFO, WARNING, ERROR, QUESTION };

    public static bool MessageBox(string title, string message, DialogType dialogType, IconType iconType, bool okDefault) {
        string dialog = dialogType.ToString ( ).ToLower ( );
        string icon = iconType.ToString ( ).ToLower ( );

        return tinyfd_messageBox ( title, message, dialog, icon, okDefault ? 1 : 0 ) == 1;
    }

    public static string InputBox(string title, string message, string defaultInput, bool password = false) {
        IntPtr strPtr = tinyfd_inputBox ( title, message, password ? null : defaultInput );

        if ( strPtr.ToInt32 ( ) == 0 )
            return null;

        return GetStringFromPtr ( strPtr );
    }

    public static string SelectFolderDialog(string title, string defaultPath) {
        IntPtr strPtr = tinyfd_selectFolderDialog ( title, defaultPath );

        if ( strPtr.ToInt32 ( ) == 0 )
            return null;

        return GetStringFromPtr ( strPtr );
    }

    public class OpenFileDialog {
        public string filePath { get { return m_Path; } }
        public string title;
        public string defaultPath;
        public string description;
        public string[] filterPatterns;
        public bool allowMultipleSelects = false;

        private string m_Path;

        public bool ShowDialog() {
            IntPtr strPtr = tinyfd_openFileDialog ( title, defaultPath, filterPatterns.Length, filterPatterns, description, allowMultipleSelects ? 1 : 0 );
            if ( strPtr.ToInt32() == 0 )
                return false;

            m_Path = GetStringFromPtr ( strPtr );

            return true;
        }

        public Stream OpenFile() {
            if ( m_Path == null || m_Path == "" )
                return null;

            return new FileStream ( m_Path, FileMode.Open );
        }
    }

    public class SaveFileDialog {
        public string filePath { get { return m_Path; } }
        public string title;
        public string defaultPath;
        public string description;
        public string[] filterPatterns;

        private string m_Path;

        public bool ShowDialog() {
            IntPtr strPtr = tinyfd_saveFileDialog ( title, defaultPath, filterPatterns.Length, filterPatterns, description );
            if ( strPtr.ToInt32 ( ) == 0 )
                return false;

            m_Path = GetStringFromPtr ( strPtr );

            bool hasFileEnding = false;
            for ( int i = 0 ; i < filterPatterns.Length ; i++ ) {
                if ( m_Path.EndsWith ( filterPatterns [ i ].Substring ( 1 ) ) )
                    hasFileEnding = true;
            }

            if ( !hasFileEnding )
                m_Path += filterPatterns [ 0 ].Substring ( 1 );

            return true;
        }

        public Stream OpenFile() {
            if ( m_Path == null || m_Path == "" )
                return null;

            return new FileStream ( m_Path, FileMode.Create );
        }
    }

    [DllImport ( "tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
    private static extern int tinyfd_messageBox(
        string aTitle,
        string aMessage,
        string aDialogType,
        string aIconType,
        int aDefaultButton
        );

    [DllImport ( "tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
    private static extern IntPtr tinyfd_inputBox(
        string aTitle,
        string aMessage,
        string aDefaultInput
        );

    [DllImport ( "tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
    private static extern IntPtr tinyfd_openFileDialog(
        string aTitle,
        string aDefaultPathAndFile,
        int aNumOfFilterPatterns,
        string [ ] aFilterPatterns,
        string aSingleFilterDescription,
        int aAllowMultipleSelects
        );

    [DllImport ( "tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
    private static extern IntPtr tinyfd_saveFileDialog(
        string aTitle,
        string aDefaultPathAndFile,
        int aNumOfFilterPatterns,
        string [ ] aFilterPatterns,
        string aSingleFilterDescription
        );

    [DllImport ( "tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
    private static extern IntPtr tinyfd_selectFolderDialog(
        string aTitle,
        string aDefaultPath
        );

    private static string GetStringFromPtr(IntPtr ptr) {
        return System.Runtime.InteropServices.Marshal.PtrToStringAnsi ( ptr );
    }
}
