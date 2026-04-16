using System.Runtime.InteropServices;

namespace RdpSolution.UI.RdpClient
{
    /// <summary>
    /// Minimal projection of IMsTscNonScriptable (IID {C1E6743A-…}).
    ///
    /// This interface is IUnknown-based (not IDispatch), so it is invisible to
    /// reflection / late-binding.  Declaring it with [ComImport] lets the CLR
    /// perform a QueryInterface automatically when the OCX is cast to this type.
    ///
    /// Every vtable slot must be declared in the correct order so that offsets
    /// are preserved.  Only put_ClearTextPassword is called by our code, but all
    /// ten members that precede ResetPassword must be present as stubs.
    /// </summary>
    [ComImport]
    [Guid("C1E6743A-41C1-4A74-932A-0156D37E84DA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMsTscNonScriptable
    {
        // slot 3 – write-only property
        void put_ClearTextPassword([In, MarshalAs(UnmanagedType.BStr)] string psz);
        // slot 4-5
        [return: MarshalAs(UnmanagedType.BStr)] string get_PortablePassword();
        void put_PortablePassword([In, MarshalAs(UnmanagedType.BStr)] string psz);
        // slot 6-7
        [return: MarshalAs(UnmanagedType.BStr)] string get_PortableSalt();
        void put_PortableSalt([In, MarshalAs(UnmanagedType.BStr)] string psz);
        // slot 8-9
        [return: MarshalAs(UnmanagedType.BStr)] string get_BinaryPassword();
        void put_BinaryPassword([In, MarshalAs(UnmanagedType.BStr)] string psz);
        // slot 10-11
        [return: MarshalAs(UnmanagedType.BStr)] string get_BinarySalt();
        void put_BinarySalt([In, MarshalAs(UnmanagedType.BStr)] string psz);
        // slot 12
        void ResetPassword();
    }
}
