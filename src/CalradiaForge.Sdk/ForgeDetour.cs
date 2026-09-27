using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Experimental low-level detouring. It is intentionally outside the supported ForgeWeave workflow.
    /// Use only in an isolated development build after validating the exact target runtime.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ForgeDetour
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const int JmpInstructionLength = 13;

        private static readonly ConcurrentDictionary<IntPtr, byte[]> _originalBytes = new ConcurrentDictionary<IntPtr, byte[]>();

        /// <summary>
        /// Redirects execution of the original method to the replacement method.
        /// </summary>
        /// <param name="original">The method to intercept.</param>
        /// <param name="replacement">The method to execute instead.</param>
        public static void Patch(MethodInfo original, MethodInfo replacement)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            // Ensure methods are JIT compiled so we can get their true function pointers
            RuntimeHelpers.PrepareMethod(original.MethodHandle);
            RuntimeHelpers.PrepareMethod(replacement.MethodHandle);

            IntPtr originalPtr = original.MethodHandle.GetFunctionPointer();
            IntPtr replacementPtr = replacement.MethodHandle.GetFunctionPointer();

            PatchMemory(originalPtr, replacementPtr);
        }

        /// <summary>
        /// Reverts a previously applied detour and restores the original machine code instructions.
        /// </summary>
        public static bool Unpatch(MethodInfo original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            IntPtr originalPtr = original.MethodHandle.GetFunctionPointer();
            return UnpatchMemory(originalPtr);
        }

        /// <summary>
        /// Unpatches all active detours and restores original memory instructions.
        /// </summary>
        public static void UnpatchAll()
        {
            foreach (var kvp in _originalBytes)
            {
                RestoreMemory(kvp.Key, kvp.Value);
            }
            _originalBytes.Clear();
        }

        /// <summary>
        /// Checks whether a method is currently detoured.
        /// </summary>
        public static bool IsPatched(MethodInfo method)
        {
            if (method == null) return false;
            IntPtr ptr = method.MethodHandle.GetFunctionPointer();
            return _originalBytes.ContainsKey(ptr);
        }

        private static void PatchMemory(IntPtr original, IntPtr replacement)
        {
            // x64 Absolute JMP instruction format:
            // MOV R11, address (10 bytes: 49 BB + 8 byte address)
            // JMP R11 (3 bytes: 41 FF E3)
            // Total: 13 bytes

            byte[] jmpInstruction = new byte[JmpInstructionLength];
            jmpInstruction[0] = 0x49; // REX.W
            jmpInstruction[1] = 0xBB; // MOV R11, imm64

            byte[] addressBytes = BitConverter.GetBytes(replacement.ToInt64());
            Array.Copy(addressBytes, 0, jmpInstruction, 2, 8);

            jmpInstruction[10] = 0x41; // REX.B
            jmpInstruction[11] = 0xFF; // JMP
            jmpInstruction[12] = 0xE3; // R11

            // Save original bytes if not already saved
            if (!_originalBytes.ContainsKey(original))
            {
                byte[] saved = new byte[JmpInstructionLength];
                Marshal.Copy(original, saved, 0, JmpInstructionLength);
                _originalBytes.TryAdd(original, saved);
            }

            // Change memory protection to allow writing
            if (!VirtualProtect(original, (UIntPtr)jmpInstruction.Length, PAGE_EXECUTE_READWRITE, out uint oldProtect))
            {
                throw new InvalidOperationException($"VirtualProtect failed with error code: {Marshal.GetLastWin32Error()}");
            }

            // Write the JMP instruction
            Marshal.Copy(jmpInstruction, 0, original, jmpInstruction.Length);

            // Restore memory protection
            VirtualProtect(original, (UIntPtr)jmpInstruction.Length, oldProtect, out _);
        }

        private static bool UnpatchMemory(IntPtr original)
        {
            if (_originalBytes.TryRemove(original, out byte[] originalBytes))
            {
                RestoreMemory(original, originalBytes);
                return true;
            }
            return false;
        }

        private static void RestoreMemory(IntPtr original, byte[] originalBytes)
        {
            if (!VirtualProtect(original, (UIntPtr)originalBytes.Length, PAGE_EXECUTE_READWRITE, out uint oldProtect))
            {
                return;
            }

            Marshal.Copy(originalBytes, 0, original, originalBytes.Length);
            VirtualProtect(original, (UIntPtr)originalBytes.Length, oldProtect, out _);
        }
    }
}
