using System;
using System.Runtime.InteropServices;
using System.Text;

namespace View621.Helpers;

/// <summary>
/// Native Windows Data Protection API (DPAPI) helper for encrypting credentials.
/// Uses CryptProtectData and CryptUnprotectData from crypt32.dll tied to the CurrentUser.
/// </summary>
public static class DpapiHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptProtectData(
        ref DATA_BLOB pDataIn,
        string szDataDescr,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn,
        IntPtr ppszDataDescr,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return null;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var inHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var inBlob = new DATA_BLOB { cbData = bytes.Length, pbData = inHandle.AddrOfPinnedObject() };
            var outBlob = new DATA_BLOB();
            var emptyBlob = new DATA_BLOB();

            try
            {
                if (CryptProtectData(ref inBlob, "View621", ref emptyBlob, IntPtr.Zero, IntPtr.Zero, 0, ref outBlob))
                {
                    var result = new byte[outBlob.cbData];
                    Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                    return Convert.ToBase64String(result);
                }
                return null;
            }
            finally
            {
                inHandle.Free();
                if (outBlob.pbData != IntPtr.Zero)
                {
                    LocalFree(outBlob.pbData);
                }
            }
        }
        catch
        {
            return null;
        }
    }

    public static string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return null;

        try
        {
            var bytes = Convert.FromBase64String(cipherText);
            var inHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var inBlob = new DATA_BLOB { cbData = bytes.Length, pbData = inHandle.AddrOfPinnedObject() };
            var outBlob = new DATA_BLOB();
            var emptyBlob = new DATA_BLOB();

            try
            {
                if (CryptUnprotectData(ref inBlob, IntPtr.Zero, ref emptyBlob, IntPtr.Zero, IntPtr.Zero, 0, ref outBlob))
                {
                    var result = new byte[outBlob.cbData];
                    Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                    return Encoding.UTF8.GetString(result);
                }
                return null;
            }
            finally
            {
                inHandle.Free();
                if (outBlob.pbData != IntPtr.Zero)
                {
                    LocalFree(outBlob.pbData);
                }
            }
        }
        catch
        {
            return null;
        }
    }
}
