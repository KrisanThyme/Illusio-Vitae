using Dalamud;
using IVPlugin.Log;
using IVPlugin.Resources.Structs;
using Penumbra.String.Classes;
using Penumbra.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using IVPlugin.Core;
using IVPlugin.Services;
using System.IO;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Resource;

namespace IVPlugin.Resources
{
    public unsafe class ResourceProcessor : IDisposable
    {
        public static ResourceProcessor Instance { get; private set; } = null!;

        public static readonly Utf8GamePath PreBoneDeformerPath =
        Utf8GamePath.FromSpan("chara/xls/boneDeformer/human.pbd"u8, out var p) ? p : Utf8GamePath.Empty;

        public delegate byte ReadFilePrototype(IntPtr fileHandler, SeFileDescriptor* fileDesc, int priority, bool isSync);
        public ReadFilePrototype ReadFile { get; private set; }

        public delegate byte ReadSqpackPrototype(IntPtr fileHandler, SeFileDescriptor* fileDesc, int priority, bool isSync);
        public Hook<ReadSqpackPrototype> ReadSqpackHook { get; private set; }

        public delegate void* GetResourceSyncPrototype(IntPtr resourceManager, uint* categoryId, ResourceType* resourceType,
            int* resourceHash, byte* path, GetResourceParameters* resParams);
        public Hook<GetResourceSyncPrototype> GetResourceSyncHook { get; private set; }

        public delegate void* GetResourceAsyncPrototype(IntPtr resourceManager, uint* categoryId, ResourceType* resourceType,
            int* resourceHash, byte* path, GetResourceParameters* resParams, bool isUnknown);
        public Hook<GetResourceAsyncPrototype> GetResourceAsyncHook { get; private set; }

        public delegate IntPtr GetFileManagerDelegate();

        private readonly GetFileManagerDelegate GetFileManager;

        private readonly GetFileManagerDelegate GetFileManager2;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate byte DecRefDelegate(IntPtr resource);

        private readonly DecRefDelegate DecRef;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void* RequestFileDelegate(IntPtr a1, IntPtr a2, IntPtr a3, byte a4);

        private readonly RequestFileDelegate RequestFile;

        private delegate nint NamedResolveDelegate(nint drawObject, nint pathBuffer, nint pathBufferSize, uint slotIndex, nint name);

        //private delegate void CharacterBaseSetupScalingDelegate(CharacterBase* drawObject, uint slotIndex);
        //private delegate void* CharacterBaseCreateDeformerDelegate(CharacterBase* drawObject, uint slotIndex);

        //private readonly Hook<CharacterBaseSetupScalingDelegate> _humanSetupScalingHook;
        //private readonly Hook<CharacterBaseCreateDeformerDelegate> _humanCreateDeformerHook;

        //[Signature(XIVSigs.HumanVTable, ScanType = ScanType.StaticAddress)]
        //public readonly nint* HumanVTable = null!;

        //[Signature(XIVSigs.CharacterUtility, ScanType = ScanType.StaticAddress)]
        //private readonly CharacterUtilityData** _characterUtilityAddress = null;

        //public CharacterUtilityData* Address
        //=> *_characterUtilityAddress;

        //public nint DefaultHumanPbdResource { get; private set; }

        public Dictionary<string, Dictionary<string, string>> replacedPaths { get; private set; } = new();

        public ResourceProcessor()
        {
            ResourceProcessor.Instance = this;

            //DalamudServices.GameInteropProvider.InitializeFromAttributes(this);

            //DefaultHumanPbdResource = (nint)Address->HumanPbdResource;

            //_humanSetupScalingHook = DalamudServices.GameInteropProvider.HookFromAddress<CharacterBaseSetupScalingDelegate>(HumanVTable[58], SetupScaling);
            //_humanCreateDeformerHook = DalamudServices.GameInteropProvider.HookFromAddress<CharacterBaseCreateDeformerDelegate>(HumanVTable[101], CreateDeformer);

            ReadSqpackHook = DalamudServices.GameInteropProvider.HookFromSignature<ReadSqpackPrototype>(XIVSigs.ReadSqpackSig, ReadSqpackDetour);
            GetResourceSyncHook = DalamudServices.GameInteropProvider.HookFromSignature<GetResourceSyncPrototype>(XIVSigs.GetResourceSyncSig, GetResourceSyncDetour);
            GetResourceAsyncHook = DalamudServices.GameInteropProvider.HookFromSignature<GetResourceAsyncPrototype>(XIVSigs.GetResourceAsyncSig, GetResourceAsyncDetour);
            ReadFile = Marshal.GetDelegateForFunctionPointer<ReadFilePrototype>(DalamudServices.SigScanner.ScanText(XIVSigs.ReadFileSig));

            ReadSqpackHook.Enable();
            GetResourceSyncHook.Enable();
            GetResourceAsyncHook.Enable();


            GetFileManager = Marshal.GetDelegateForFunctionPointer<GetFileManagerDelegate>(DalamudServices.SigScanner.ScanText(XIVSigs.GetFileManagerSig));
            GetFileManager2 = Marshal.GetDelegateForFunctionPointer<GetFileManagerDelegate>(DalamudServices.SigScanner.ScanText(XIVSigs.GetFileManager2Sig));
            DecRef = Marshal.GetDelegateForFunctionPointer<DecRefDelegate>(DalamudServices.SigScanner.ScanText(XIVSigs.DecRefSig));
            RequestFile = Marshal.GetDelegateForFunctionPointer<RequestFileDelegate>(DalamudServices.SigScanner.ScanText(XIVSigs.RequestFileSig));
        }

        //public void EnableHumanPBD()
        //{
        //    _humanSetupScalingHook.Enable();
        //    _humanCreateDeformerHook.Enable();
        //}

        //public void DisableHumanPBD()
        //{
        //    _humanSetupScalingHook.Disable();
        //    _humanCreateDeformerHook.Disable();
        //}

        public void AddReplacePath(string ModName, Dictionary<string, string> replacements, bool forceRequest = false)
        {
            if (replacedPaths.TryGetValue(ModName, out var replacedPath))
            {
                replacedPaths[ModName] = replacements;
            }
            else
            {
                replacedPaths.Add(ModName, replacements);
            }

            foreach (var path in replacements)
            {
                var source = path.Key;
                var replacement = path.Value;

                source = source.Replace("\\", "/");
                replacement = replacement.Replace("\\", "/");

                //ReloadPath(source, null, null, null);

                if (Path.GetExtension(source) == ".pap" || Path.GetExtension(source) == ".tmb" || forceRequest)
                    ReloadPath(source, replacement, null, null);
            }
        }

        public void RemoveReplacePath(string modName)
        {
            if (replacedPaths.TryGetValue(modName, out var result))
            {
                replacedPaths.Remove(modName);

                foreach (var key in result.Keys)
                {
                    ReloadPath(key, null, null, null);
                }
            }
        }

        public bool CheckForReplacement(string gPath, out string replacementPath)
        {
            foreach (var key in replacedPaths.Values)
            {
                foreach (var rPath in key.Keys)
                {
                    if (gPath == rPath)
                    {
                        replacementPath = key[rPath];
                        return true;
                    }
                }
            }

            replacementPath = null;
            return false;
        }

        #region Detours

        private byte ReadSqpackDetour(IntPtr fileHandler, SeFileDescriptor* fileDesc, int priority, bool isSync)
        {
            if (fileDesc->ResourceHandle == null) return ReadSqpackHook.Original(fileHandler, fileDesc, priority, isSync);

            if (!fileDesc->ResourceHandle->GamePath(out var originalGamePath))
            {
                return ReadSqpackHook.Original(fileHandler, fileDesc, priority, isSync);
            }

            IllusioDebug.Log($"[SqpackReader] Loading File {originalGamePath}", LogType.Verbose);

            if (!CheckForReplacement(originalGamePath.ToString(), out var replacedPath))
            {
                return ReadSqpackHook.Original(fileHandler, fileDesc, priority, isSync);
            }

            fileDesc->FileMode = Structs.FileMode.LoadUnpackedResource;

            ByteString.FromString(replacedPath, out var gamePath);

            var utfPath = Encoding.Unicode.GetBytes(replacedPath);
            Marshal.Copy(utfPath, 0, new IntPtr(&fileDesc->Utf16FileName), utfPath.Length);
            var fd = stackalloc byte[0x20 + utfPath.Length + 0x16];
            Marshal.Copy(utfPath, 0, new IntPtr(fd + 0x21), utfPath.Length);
            fileDesc->FileDescriptor = fd;

            IllusioDebug.Log($"[SqpackReader] Replaced File {originalGamePath} with {replacedPath}", LogType.Verbose);

            return ReadFile(fileHandler, fileDesc, priority, isSync);
        }

        private void* GetResourceSyncDetour(
            IntPtr resourceManager,
            uint* categoryId,
            ResourceType* resourceType,
            int* resourceHash,
            byte* path,
            GetResourceParameters* resParams
        ) => GetResourceHandler(true, resourceManager, categoryId, resourceType, resourceHash, path, resParams, false);

        private void* GetResourceAsyncDetour(
            IntPtr resourceManager,
            uint* categoryId,
            ResourceType* resourceType,
            int* resourceHash,
            byte* path,
            GetResourceParameters* resParams,
            bool isUnknown
        ) => GetResourceHandler(false, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);

        private void* GetResourceHandler(
            bool isSync,
            IntPtr resourceManager,
            uint* categoryId,
            ResourceType* resourceType,
            int* resourceHash,
            byte* path,
            GetResourceParameters* resParams,
            bool isUnknown
        )
        {
            if (!Utf8GamePath.FromPointer(path, out var gamePath))
            {
                return CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);
            }

            IllusioDebug.Log($"[ResourceHandlerReader] Loading File {gamePath}", LogType.Verbose);

            if (!CheckForReplacement(gamePath.ToString(), out var replacedPath))
            {
                return CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);
            }

            var x = ByteString.FromString(replacedPath.Replace('\\', '/'), out var name, true) ? name : ByteString.Empty;

            *resourceHash = ComputeHash(x, resParams);
            path = x.Path;

            IllusioDebug.Log($"[ResourceHandlerReader] Replacing File {gamePath} with {replacedPath}", LogType.Verbose);

            return CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);
        }

        private void* CallOriginalHandler(
            bool isSync,
            IntPtr resourceManager,
            uint* categoryId,
            ResourceType* resourceType,
            int* resourceHash,
            byte* path,
            GetResourceParameters* resParams,
            bool isUnknown
        ) => isSync
            ? GetResourceSyncHook.Original(resourceManager, categoryId, resourceType, resourceHash, path, resParams)
            : GetResourceAsyncHook.Original(resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);


        public static int ComputeHash(ByteString path, GetResourceParameters* resParams)
        {
            if (resParams == null || !resParams->IsPartialRead) return path.Crc32;

            return ByteString.Join(
                (byte)'.',
                path,
                ByteString.FromStringUnsafe(resParams->SegmentOffset.ToString("x"), true),
                ByteString.FromStringUnsafe(resParams->SegmentLength.ToString("x"), true)
            ).Crc32;
        }

        public void ReloadPath(string gamePath, string localPath, List<string> papIds, List<short> papTypes)
        {
            if (string.IsNullOrEmpty(gamePath)) return;

            var gameResource = GetResource(gamePath, true);

            if (gameResource != IntPtr.Zero)
            {
                PrepPap(gameResource, papIds, papTypes);
                RequestFile(GetFileManager2(), gameResource + Constants.GameResourceOffset, gameResource, 1);
                WritePapIds(gameResource, papIds, papTypes);
            }

            if (string.IsNullOrEmpty(localPath)) return;

            var localGameResource = GetResource(gamePath, false); // get local path resource

            if (localGameResource != IntPtr.Zero)
            {
                PrepPap(localGameResource, papIds, papTypes);
                RequestFile(GetFileManager2(), localGameResource + Core.Constants.GameResourceOffset, localGameResource, 1);
                WritePapIds(localGameResource, papIds, papTypes);
            }
        }

        public static void PrepPap(IntPtr resource, List<string> ids, List<short> types)
        {
            if (ids == null || types == null) return;
            Marshal.WriteByte(resource + Core.Constants.PrepPapOffset, Core.Constants.PrepPapValue);
        }

        public static void WritePapIds(IntPtr resource, List<string> ids, List<short> types)
        {
            if (ids == null) return;
            var data = Marshal.ReadIntPtr(resource + Constants.PapIdsOffset);
            for (var i = 0; i < ids.Count; i++)
            {
                SafeMemory.WriteString(data + (i * 40), ids[i], Encoding.ASCII);
                Marshal.WriteInt16(data + (i * 40) + 32, types[i]);
                Marshal.WriteByte(data + (i * 40) + 34, (byte)i);
            }
        }

        private IntPtr GetResource(string path, bool original)
        {
            var extension = Reverse(path.Split('.')[1]);
            var typeBytes = Encoding.ASCII.GetBytes(extension);
            var bType = stackalloc byte[typeBytes.Length + 1];
            Marshal.Copy(typeBytes, 0, new IntPtr(bType), typeBytes.Length);
            var pResourceType = (ResourceType*)bType;

            // Category
            var split = path.Split('/');
            var categoryString = split[0];
            var categoryBytes = categoryString switch
            {
                "bgcommon" => BitConverter.GetBytes(1u),
                "cur" => GetDatCategory(3u, split[1]),
                "chara" => BitConverter.GetBytes(4u),
                "shader" => BitConverter.GetBytes(5u),
                "ui" => BitConverter.GetBytes(6u),
                "sound" => BitConverter.GetBytes(7u),
                "vfx" => BitConverter.GetBytes(8u),
                "bg" => GetBgCategory(split[1], split[2]),
                "music" => GetDatCategory(12u, split[1]),
                _ => BitConverter.GetBytes(0u)
            };
            var bCategory = stackalloc byte[categoryBytes.Length + 1];
            Marshal.Copy(categoryBytes, 0, new IntPtr(bCategory), categoryBytes.Length);
            var pCategoryId = (uint*)bCategory;

            ByteString.FromString(path, out var resolvedPath);
            var hash = resolvedPath.GetHashCode();

            var hashBytes = BitConverter.GetBytes(hash);
            var bHash = stackalloc byte[hashBytes.Length + 1];
            Marshal.Copy(hashBytes, 0, new IntPtr(bHash), hashBytes.Length);
            var pResourceHash = (int*)bHash;

            var resource = original ? new IntPtr(GetResourceSyncHook.Original(GetFileManager(), pCategoryId, pResourceType, pResourceHash, resolvedPath.Path, null)) :
                new IntPtr(GetResourceSyncDetour(GetFileManager(), pCategoryId, pResourceType, pResourceHash, resolvedPath.Path, null));
            DecRef(resource);

            return resource;
        }

        public static string Reverse(string data) => new(data.ToCharArray().Reverse().ToArray());

        public static byte[] GetDatCategory(uint prefix, string expansion)
        {
            var ret = BitConverter.GetBytes(prefix);
            if (expansion == "ffxiv") return ret;
            // music/ex4/BGM_EX4_Field_Ult_Day03.scd
            // 04 00 00 0C
            var expansionTrimmed = expansion.Replace("ex", "");
            ret[3] = byte.Parse(expansionTrimmed);
            return ret;
        }

        public static byte[] GetBgCategory(string expansion, string zone)
        {
            var ret = BitConverter.GetBytes(2u);
            if (expansion == "ffxiv") return ret;
            // ex1/03_abr_a2/fld/a2f1/level/a2f1 -> [02 00 03 01]
            // expansion = ex1
            // zone = 03_abr_a2
            var expansionTrimmed = expansion.Replace("ex", "");
            var zoneTrimmed = zone.Split('_')[0];
            ret[2] = byte.Parse(zoneTrimmed);
            ret[3] = byte.Parse(expansionTrimmed);
            return ret;
        }
        
        //private SafeResourceHandle GetPreBoneDeformerForCharacter(CharacterBase* drawObject)
        //{
        //    var resolveData = _collectionResolver.IdentifyCollection(&drawObject->DrawObject, true);
        //    if (resolveData.ModCollection._cache is not { } cache)
        //        return _resourceLoader.LoadResolvedSafeResource(ResourceCategory.Chara, ResourceType.Pbd, PreBoneDeformerPath.Path, resolveData);

        //    return cache.CustomResources.Get(ResourceCategory.Chara, ResourceType.Pbd, PreBoneDeformerPath, resolveData);
        //}

        //private void SetupScaling(CharacterBase* drawObject, uint slotIndex)
        //{
        //    var deformer = GetResource("chara/xls/boneDeformer/human.pbd", false);

        //    try
        //    {
        //        Address->HumanPbdResource = (Structs.ResourceHandle*)deformer;
        //        _humanSetupScalingHook.Original(drawObject, slotIndex);
        //    }
        //    finally
        //    {
        //        Address->HumanPbdResource = (Structs.ResourceHandle*)deformer;
        //    }
        //}

        //private void* CreateDeformer(CharacterBase* drawObject, uint slotIndex)
        //{
        //    var deformer = GetResource("chara/xls/boneDeformer/human.pbd", false);

        //    try
        //    {
        //        Address->HumanPbdResource = (Structs.ResourceHandle*)deformer;
        //        return _humanCreateDeformerHook.Original(drawObject, slotIndex);
        //    }
        //    finally
        //    {
        //        Address->HumanPbdResource = (Structs.ResourceHandle*)deformer;
        //    }
        //}

        #endregion

        public void Dispose()
        {
            ReadSqpackHook.Dispose();
            GetResourceSyncHook.Dispose();
            GetResourceAsyncHook.Dispose();
            //_humanSetupScalingHook.Dispose();
            //_humanCreateDeformerHook.Dispose();
        }
    }
}
