using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    [ComImport,
	Guid("0C9FB851-E5C9-43EB-A370-F0677B13874C"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IOperationsProgressDialog
	{
		//virtual HRESULT STDMETHODCALLTYPE StartProgressDialog( 
		//    /* [unique][in] */ __RPC__in_opt HWND hwndParent,
		//    /* [in] */ DWORD dwFlags) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void StartProgressDialog([In] IntPtr hwndParent, [In] DialogMode dwFlags);

		//virtual HRESULT STDMETHODCALLTYPE StopProgressDialog( void) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void StopProgressDialog();

		//virtual HRESULT STDMETHODCALLTYPE SetOperation( 
		//    /* [in] */ SPACTION action) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOperation([In] OperationAction action);

		//virtual HRESULT STDMETHODCALLTYPE SetMode( 
		//    /* [in] */ PDMODE mode) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetMode([In] OperationMode mode);

		//virtual HRESULT STDMETHODCALLTYPE UpdateProgress( 
		//    /* [in] */ ULONGLONG ullPointsCurrent,
		//    /* [in] */ ULONGLONG ullPointsTotal,
		//    /* [in] */ ULONGLONG ullSizeCurrent,
		//    /* [in] */ ULONGLONG ullSizeTotal,
		//    /* [in] */ ULONGLONG ullItemsCurrent,
		//    /* [in] */ ULONGLONG ullItemsTotal) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void UpdateProgress([In] ulong ullPointsCurrent,
												[In] ulong ullPointsTotal,
												[In] ulong ullSizeCurrent,
												[In] ulong ullSizeTotal,
												[In] ulong ullItemsCurrent,
												[In] ulong ullItemsTotal);

		//virtual HRESULT STDMETHODCALLTYPE UpdateLocations( 
		//    /* [unique][in] */ __RPC__in_opt IShellItem *psiSource,
		//    /* [unique][in] */ __RPC__in_opt IShellItem *psiTarget,
		//    /* [unique][in] */ __RPC__in_opt IShellItem *psiItem) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void UpdateLocations([In] IntPtr psiSource,
												 [In] IntPtr psiTarget,
												 [In] IntPtr psiItem);

		//virtual HRESULT STDMETHODCALLTYPE ResetTimer( void) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ResetTimer();

		//virtual HRESULT STDMETHODCALLTYPE PauseTimer( void) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void PauseTimer();

		//virtual HRESULT STDMETHODCALLTYPE ResumeTimer( void) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ResumeTimer();

		//virtual HRESULT STDMETHODCALLTYPE GetMilliseconds( 
		//    /* [out] */ __RPC__out ULONGLONG *pullElapsed,
		//    /* [out] */ __RPC__out ULONGLONG *pullRemaining) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetMilliseconds([Out] out ulong pullElapsed, [Out] out ulong pullRemaining);

		//virtual HRESULT STDMETHODCALLTYPE GetOperationStatus( 
		//    /* [out] */ __RPC__out PDOPSTATUS *popstatus) = 0;
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOperationStatus([Out] out OperationStatus popsStatus);
	}
}
