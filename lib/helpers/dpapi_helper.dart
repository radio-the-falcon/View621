import 'dart:convert';
import 'dart:ffi';
import 'dart:io';
import 'package:ffi/ffi.dart';

final class DataBlob extends Struct {
  @Uint32()
  external int cbData;
  external Pointer<Uint8> pbData;
}

typedef CryptProtectDataNative = Int32 Function(
  Pointer<DataBlob> pDataIn,
  Pointer<Utf16> szDataDescr,
  Pointer<DataBlob> pOptionalEntropy,
  Pointer<Void> pvReserved,
  Pointer<Void> pPromptStruct,
  Uint32 dwFlags,
  Pointer<DataBlob> pDataOut,
);
typedef CryptProtectDataDart = int Function(
  Pointer<DataBlob> pDataIn,
  Pointer<Utf16> szDataDescr,
  Pointer<DataBlob> pOptionalEntropy,
  Pointer<Void> pvReserved,
  Pointer<Void> pPromptStruct,
  int dwFlags,
  Pointer<DataBlob> pDataOut,
);

typedef CryptUnprotectDataNative = Int32 Function(
  Pointer<DataBlob> pDataIn,
  Pointer<Pointer<Utf16>> ppszDataDescr,
  Pointer<DataBlob> pOptionalEntropy,
  Pointer<Void> pvReserved,
  Pointer<Void> pPromptStruct,
  Uint32 dwFlags,
  Pointer<DataBlob> pDataOut,
);
typedef CryptUnprotectDataDart = int Function(
  Pointer<DataBlob> pDataIn,
  Pointer<Pointer<Utf16>> ppszDataDescr,
  Pointer<DataBlob> pOptionalEntropy,
  Pointer<Void> pvReserved,
  Pointer<Void> pPromptStruct,
  int dwFlags,
  Pointer<DataBlob> pDataOut,
);

typedef LocalFreeNative = Pointer<Void> Function(Pointer<Void> hMem);
typedef LocalFreeDart = Pointer<Void> Function(Pointer<Void> hMem);

/// Native Windows Data Protection API (DPAPI) helper for encrypting credentials.
/// Compatible with View621 C# implementation.
class DpapiHelper {
  static DynamicLibrary? _crypt32;
  static DynamicLibrary? _kernel32;
  static CryptProtectDataDart? _cryptProtectData;
  static CryptUnprotectDataDart? _cryptUnprotectData;
  static LocalFreeDart? _localFree;

  static void _init() {
    if (!Platform.isWindows) return;
    _crypt32 ??= DynamicLibrary.open('crypt32.dll');
    _kernel32 ??= DynamicLibrary.open('kernel32.dll');

    _cryptProtectData ??= _crypt32!.lookupFunction<CryptProtectDataNative, CryptProtectDataDart>('CryptProtectData');
    _cryptUnprotectData ??= _crypt32!.lookupFunction<CryptUnprotectDataNative, CryptUnprotectDataDart>('CryptUnprotectData');
    _localFree ??= _kernel32!.lookupFunction<LocalFreeNative, LocalFreeDart>('LocalFree');
  }

  static String? encrypt(String? plainText) {
    if (plainText == null || plainText.isEmpty) return null;
    if (!Platform.isWindows) return plainText;

    try {
      _init();
      if (_cryptProtectData == null || _localFree == null) return null;

      final utf8Bytes = utf8.encode(plainText);
      final inData = calloc<Uint8>(utf8Bytes.length);
      for (var i = 0; i < utf8Bytes.length; i++) {
        inData[i] = utf8Bytes[i];
      }

      final inBlob = calloc<DataBlob>();
      inBlob.ref.cbData = utf8Bytes.length;
      inBlob.ref.pbData = inData;

      final emptyBlob = calloc<DataBlob>();
      emptyBlob.ref.cbData = 0;
      emptyBlob.ref.pbData = nullptr;

      final outBlob = calloc<DataBlob>();
      final descr = 'View621'.toNativeUtf16();

      try {
        final success = _cryptProtectData!(
          inBlob,
          descr,
          emptyBlob,
          nullptr,
          nullptr,
          0,
          outBlob,
        );

        if (success != 0 && outBlob.ref.pbData != nullptr) {
          final outBytes = outBlob.ref.pbData.asTypedList(outBlob.ref.cbData);
          final base64String = base64Encode(outBytes);
          return base64String;
        }
        return null;
      } finally {
        if (outBlob.ref.pbData != nullptr) {
          _localFree!(outBlob.ref.pbData.cast());
        }
        calloc.free(descr);
        calloc.free(inData);
        calloc.free(inBlob);
        calloc.free(emptyBlob);
        calloc.free(outBlob);
      }
    } catch (_) {
      return null;
    }
  }

  static String? decrypt(String? cipherText) {
    if (cipherText == null || cipherText.isEmpty) return null;
    if (!Platform.isWindows) return cipherText;

    try {
      _init();
      if (_cryptUnprotectData == null || _localFree == null) return null;

      final cipherBytes = base64Decode(cipherText);
      final inData = calloc<Uint8>(cipherBytes.length);
      for (var i = 0; i < cipherBytes.length; i++) {
        inData[i] = cipherBytes[i];
      }

      final inBlob = calloc<DataBlob>();
      inBlob.ref.cbData = cipherBytes.length;
      inBlob.ref.pbData = inData;

      final emptyBlob = calloc<DataBlob>();
      emptyBlob.ref.cbData = 0;
      emptyBlob.ref.pbData = nullptr;

      final outBlob = calloc<DataBlob>();

      try {
        final success = _cryptUnprotectData!(
          inBlob,
          nullptr,
          emptyBlob,
          nullptr,
          nullptr,
          0,
          outBlob,
        );

        if (success != 0 && outBlob.ref.pbData != nullptr) {
          final outBytes = outBlob.ref.pbData.asTypedList(outBlob.ref.cbData);
          return utf8.decode(outBytes);
        }
        return null;
      } finally {
        if (outBlob.ref.pbData != nullptr) {
          _localFree!(outBlob.ref.pbData.cast());
        }
        calloc.free(inData);
        calloc.free(inBlob);
        calloc.free(emptyBlob);
        calloc.free(outBlob);
      }
    } catch (_) {
      return null;
    }
  }
}
