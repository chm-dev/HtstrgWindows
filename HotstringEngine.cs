using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace HtstrgWindows
{
    public class Hotstring : INotifyPropertyChanged
    {
        private string _trigger = "";
        private string _replacement = "";
        private bool _caseSensitive = false;
        private bool _expandImmediately = false;
        private bool _omitEndChar = false;

        public string Trigger 
        { 
            get => _trigger; 
            set { _trigger = value; OnPropertyChanged(); } 
        }
        
        public string Replacement 
        { 
            get => _replacement; 
            set { _replacement = value; OnPropertyChanged(); } 
        }
        
        public bool CaseSensitive 
        { 
            get => _caseSensitive; 
            set { _caseSensitive = value; OnPropertyChanged(); } 
        }
        
        public bool ExpandImmediately 
        { 
            get => _expandImmediately; 
            set { _expandImmediately = value; OnPropertyChanged(); } 
        }
        
        public bool OmitEndChar 
        { 
            get => _omitEndChar; 
            set { _omitEndChar = value; OnPropertyChanged(); } 
        }

public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null!)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name!));
            }
        }
    }

    public class HotstringEngine : IDisposable
    {
        private NativeMethods.LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private StringBuilder _buffer = new StringBuilder();
        public ObservableCollection<Hotstring> Hotstrings { get; } = new ObservableCollection<Hotstring>();
        public bool IsPaused { get; set; } = false;
        private bool _sendingInput = false;
        private byte[] _keyState = new byte[256];

        // End chars: Space, enter, tab, punctuation
        private readonly HashSet<char> _endChars = new HashSet<char>() 
        { 
            ' ', '\n', '\r', '\t', '-', '(', ')', '[', ']', '{', '}', ':', ';', '"', '\'', ',', '.', '<', '>', '/', '?', '\\', '!' 
        };

        public HotstringEngine()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void AddHotstring(string trigger, string replacement)
        {
            Hotstrings.Add(new Hotstring { Trigger = trigger, Replacement = replacement });
        }

        public void ClearHotstrings()
        {
            Hotstrings.Clear();
        }

private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc,
                    NativeMethods.GetModuleHandle(curModule?.ModuleName ?? string.Empty), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // If we are sending input or paused, ignore hook to avoid loops
            if (_sendingInput || IsPaused)
                return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);

            if (nCode >= 0)
            {
                bool keyDown = (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN);
                bool keyUp = (wParam == (IntPtr)NativeMethods.WM_KEYUP || wParam == (IntPtr)NativeMethods.WM_SYSKEYUP);

                if (keyDown || keyUp)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    
                    // Update internal key state
                    if (vkCode >= 0 && vkCode < 256)
                    {
                        if (keyDown)
                            _keyState[vkCode] |= 0x80; // Set high bit
                        else
                            _keyState[vkCode] &= 0x7F; // Clear high bit
                            
                        // Update generic modifiers
                        UpdateGenericModifier(160, 161, 16); // Shift
                        UpdateGenericModifier(162, 163, 17); // Ctrl
                        UpdateGenericModifier(164, 165, 18); // Alt
                    }

                    if (keyDown)
                    {
                         // Handle special keys that reset buffer
                        if (IsNavigationKey(vkCode))
                        {
                            _buffer.Clear();
                            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
                        }

                        if (vkCode == 0x08) // Backspace
                        {
                            if (_buffer.Length > 0)
                                _buffer.Length--;
                            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
                        }

                        // Convert to char
                        char? c = GetCharFromKey(vkCode);
                        if (c.HasValue)
                        {
                            // Trigger Handling Logic
                            bool isEndChar = _endChars.Contains(c.Value);
                            
                            // 1. Check EndChar Matches
                            if (isEndChar)
                            {
                                var match = Hotstrings.FirstOrDefault(h => 
                                    !h.ExpandImmediately && 
                                    BufferEndsWith(h.Trigger, h.CaseSensitive));
                                
                                if (match != null)
                                {
                                    HandleReplacement(match, c.Value);
                                    return (IntPtr)1; // Block input
                                }
                            }
                            
                            // 2. Append to buffer
                            _buffer.Append(c.Value);
                            if (_buffer.Length > 200) _buffer.Remove(0, 100); 

                            // 3. Check Immediate Matches
                            var immediateMatch = Hotstrings.FirstOrDefault(h => 
                                h.ExpandImmediately && 
                                BufferEndsWith(h.Trigger, h.CaseSensitive));
                                
                            if (immediateMatch != null)
                            {
                                 HandleReplacement(immediateMatch, null);
                                 return (IntPtr)1; // Block input
                            }
                        }
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void UpdateGenericModifier(int left, int right, int generic)
        {
            bool l = (_keyState[left] & 0x80) != 0;
            bool r = (_keyState[right] & 0x80) != 0;
            if (l || r) _keyState[generic] |= 0x80;
            else _keyState[generic] &= 0x7F;
        }

        private bool BufferEndsWith(string trigger, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(trigger)) return false;
            if (_buffer.Length < trigger.Length) return false;

            string tail = _buffer.ToString().Substring(_buffer.Length - trigger.Length);
            return string.Equals(tail, trigger, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }

        private void HandleReplacement(Hotstring hotstring, char? endChar)
        {
            _sendingInput = true;
            try
            {
                int backspaces = hotstring.Trigger.Length;
                if (endChar == null) // Immediate mode, last char blocked
                {
                    backspaces -= 1; 
                }
                
                SendBackspaces(backspaces);
                SendString(hotstring.Replacement);
                
                if (endChar.HasValue && !hotstring.OmitEndChar)
                {
                    SendChar(endChar.Value);
                }
                
                _buffer.Clear();
            }
            finally
            {
                _sendingInput = false;
            }
        }

        private void SendBackspaces(int count)
        {
            if (count <= 0) return;
            var inputs = new List<NativeMethods.INPUT>();
            for (int i = 0; i < count; i++)
            {
                inputs.Add(CreateKeyInput(0x08, false)); // VK_BACK down
                inputs.Add(CreateKeyInput(0x08, true));  // VK_BACK up
            }
            NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        private void SendString(string text)
        {
            var inputs = new List<NativeMethods.INPUT>();
            foreach (char c in text)
            {
                inputs.Add(CreateUnicodeInput(c, false));
                inputs.Add(CreateUnicodeInput(c, true));
            }
             NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
        
        private void SendChar(char c)
        {
             var inputs = new NativeMethods.INPUT[] 
             { 
                 CreateUnicodeInput(c, false), 
                 CreateUnicodeInput(c, true) 
             };
             NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        private NativeMethods.INPUT CreateKeyInput(ushort wVk, bool keyUp)
        {
            var input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.U.ki.wVk = wVk;
            input.U.ki.dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0;
            return input;
        }

        private NativeMethods.INPUT CreateUnicodeInput(char c, bool keyUp)
        {
            var input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.U.ki.wScan = c;
            input.U.ki.dwFlags = NativeMethods.KEYEVENTF_UNICODE | (uint)(keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0);
            return input;
        }

        private char? GetCharFromKey(int vkCode)
        {
            // Use ToUnicode with our manually tracked state
            
            // Explicitly handle Enter/Tab/Space for safety, 
            // though ToUnicode usually handles them well (Space -> 32, Enter -> 13).
            // But let's trust ToUnicode first?
            // ToUnicode might return control characters for some combos.
            
            StringBuilder sb = new StringBuilder(2);
            // ScanCode 0 is often ok.
            if (NativeMethods.ToUnicode((uint)vkCode, 0, _keyState, sb, sb.Capacity, 0) > 0)
            {
                char c = sb[0];
                if (vkCode == 13) return '\n'; // Normalize Enter
                if (!char.IsControl(c) || char.IsWhiteSpace(c))
                {
                    return c;
                }
            }
            return null;
        }

        private bool IsNavigationKey(int vkCode)
        {
            // Arrows (37-40), Insert(45), Delete(46), Home(36), End(35), PgUp(33), PgDn(34)
            return (vkCode >= 37 && vkCode <= 40) || vkCode == 45 || vkCode == 46 || vkCode == 36 || vkCode == 35 || vkCode == 33 || vkCode == 34;
        }

        public void Dispose()
        {
            NativeMethods.UnhookWindowsHookEx(_hookID);
        }
    }
}
