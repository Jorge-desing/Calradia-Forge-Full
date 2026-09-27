using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CalradiaForge.Sdk.Patcher
{
    public static class MethodSwapper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        /// <summary>
        /// Swaps the target method with the replacement method using an x64 JMP detour.
        /// Returns the original 13 bytes so the patch can be reverted dynamically.
        /// </summary>
        public static unsafe byte[] DetourMethod(MethodInfo target, MethodInfo replacement)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            // Force the JIT compiler to compile both methods
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            RuntimeHelpers.PrepareMethod(replacement.MethodHandle);

            IntPtr targetPtr = target.MethodHandle.GetFunctionPointer();
            IntPtr replacementPtr = replacement.MethodHandle.GetFunctionPointer();

            // 13 bytes needed for an absolute JMP in x64:
            // 49 BB <8 byte address>   (mov r11, address)
            // 41 FF E3                 (jmp r11)
            UIntPtr size = (UIntPtr)13;

            // Unprotect memory to allow writing
            if (!VirtualProtect(targetPtr, size, 0x40 /* PAGE_EXECUTE_READWRITE */, out uint oldProtect))
            {
                throw new InvalidOperationException("Failed to unprotect memory for method detour.");
            }

            byte* ptr = (byte*)targetPtr;
            
            // Backup the original 13 bytes
            byte[] originalBytes = new byte[13];
            for (int i = 0; i < 13; i++)
            {
                originalBytes[i] = ptr[i];
            }

            // Write the JMP instruction
            *ptr = 0x49; 
            *(ptr + 1) = 0xBB; 
            *(long*)(ptr + 2) = replacementPtr.ToInt64();
            *(ptr + 10) = 0x41; 
            *(ptr + 11) = 0xFF; 
            *(ptr + 12) = 0xE3;

            // Restore memory protection
            VirtualProtect(targetPtr, size, oldProtect, out _);
            
            return originalBytes;
        }

        /// <summary>
        /// Restores a method to its original state using the backed-up assembly bytes.
        /// </summary>
        public static unsafe void RestoreMethod(MethodInfo target, byte[] originalBytes)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (originalBytes == null || originalBytes.Length != 13) throw new ArgumentException("Invalid backup bytes.");

            IntPtr targetPtr = target.MethodHandle.GetFunctionPointer();
            UIntPtr size = (UIntPtr)13;

            if (!VirtualProtect(targetPtr, size, 0x40 /* PAGE_EXECUTE_READWRITE */, out uint oldProtect))
            {
                throw new InvalidOperationException("Failed to unprotect memory for method restoration.");
            }

            byte* ptr = (byte*)targetPtr;
            for (int i = 0; i < 13; i++)
            {
                ptr[i] = originalBytes[i];
            }

            VirtualProtect(targetPtr, size, oldProtect, out _);
        }
    }
}
