using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using IVPlugin.Core;
using IVPlugin.Env.Structs;
using IVPlugin.Resources;
using IVPlugin.Services;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Gpose
{
    public class WorldManager : IDisposable
    {
        public static WorldManager Instance { get; private set; }

        private delegate void UpdateEorzeaTimeDelegate(IntPtr a1, IntPtr a2);
        private readonly Hook<UpdateEorzeaTimeDelegate> _updateEorzeaTimeHook = null!;

        private delegate void UpdateTerritoryWeatherDelegate(IntPtr a1, IntPtr a2);
        private readonly Hook<UpdateTerritoryWeatherDelegate> _updateTerritoryWeatherHook = null!;

        private delegate nint SkyTexDelegate(nint a1, nint a2);
        private static Hook<SkyTexDelegate> SkyTexHook = null!;

        public bool WeatherOverrideEnabled
        {
            get => _updateTerritoryWeatherHook.IsEnabled;
            set
            {
                if (value != WeatherOverrideEnabled)
                {
                    if (value)
                    {
                        originalWeather = CurrentWeather;
                        _updateTerritoryWeatherHook.Enable();
                    }
                    else
                    {
                        CurrentWeather = originalWeather;
                        _updateTerritoryWeatherHook.Disable();
                    }
                }
            }
        }

        public bool IsTimeFrozen
        {
            get => _updateEorzeaTimeHook.IsEnabled;
            set
            {

                if (value != IsTimeFrozen)
                {
                    if (value)
                    {
                        _updateEorzeaTimeHook.Enable();
                    }
                    else
                    {
                        _updateEorzeaTimeHook.Disable();
                    }
                }
            }
        }

        public unsafe long EorzeaTime
        {
            get
            {
                var framework = Framework.Instance();
                if (framework == null) return 0;
                return framework->ClientTime.IsEorzeaTimeOverridden ? framework->ClientTime.EorzeaTimeOverride : framework->ClientTime.EorzeaTime;
            }

            set
            {
                var framework = Framework.Instance();
                if (framework == null) return;
                framework->ClientTime.EorzeaTime = value;
                if (framework->ClientTime.IsEorzeaTimeOverridden)
                    framework->ClientTime.EorzeaTimeOverride = value;
            }
        }

        public int MinuteOfDay
        {
            get
            {
                long currentTime = EorzeaTime;
                long timeVal = currentTime % 2764800;
                long secondInDay = timeVal % 86400;
                int minuteOfDay = (int)(secondInDay / 60f);
                return minuteOfDay;
            }

            set
            {
                EorzeaTime = value * 60 + 86400 * ((byte)DayOfMonth - 1);
            }
        }

        public int DayOfMonth
        {
            get
            {
                long currentTime = EorzeaTime;
                long timeVal = currentTime % 2764800;
                int dayOfMonth = (int)(Math.Floor(timeVal / 86400f) + 1);
                return dayOfMonth;
            }

            set
            {
                EorzeaTime = MinuteOfDay * 60 + 86400 * ((byte)value - 1);
            }
        }

        public ReadOnlyCollection<Weather> TerritoryWeatherTable
        {
            get
            {
                //if (_currentCachedTerritory != DalamudServices.clientState.TerritoryType)
                //{
                    _currentCachedTerritory = 0;
                    UpdateWeathersForCurrentTerritory();
                    if (_territoryWeatherTable.Any())
                        _currentCachedTerritory = DalamudServices.clientState.TerritoryType;
                //}

                return _territoryWeatherTable.AsReadOnly();
            }
        }

        public unsafe int CurrentWeather
        {
            get
            {
                var system = EnvManager.Instance();
                if (system == null) return 0;
                return system->ActiveWeather;
            }
            set
            {
                var system = EnvManager.Instance();
                if (system != null)
                {
                    system->ActiveWeather = (byte)value;
                    system->TransitionTime = DefaultTransitionTime;
                }
            }
        }

        public unsafe uint CurrentSky
        {
            get
            {
                var env = (ExtendedEnviornmentManager*)EnvManager.Instance();
                return env->SkyId;
            }
            set
            {
                if(value != CurrentSky)
                    SkyTexHook.Enable();
                NewSkyID = value;
            }
        }

        private const float DefaultTransitionTime = 0.5f;

        private readonly List<Weather> _territoryWeatherTable = [];

        private ushort? _currentCachedTerritory;

        private int originalWeather;
        private uint originalSkyID;
        private uint NewSkyID;

        public IEnumerable<Weather> AllWeatherCollection => GameResourceManager.Instance.Weathers.Values;

        public WorldManager() 
        {
            Instance = this;

            var etAddress = DalamudServices.SigScanner.ScanText(XIVSigs.environmentTime);
            _updateEorzeaTimeHook = DalamudServices.GameInteropProvider.HookFromAddress<UpdateEorzeaTimeDelegate>(etAddress, UpdateEorzeaTime);

            var twAddress = DalamudServices.SigScanner.ScanText(XIVSigs.environmentWeather);
            _updateTerritoryWeatherHook = DalamudServices.GameInteropProvider.HookFromAddress<UpdateTerritoryWeatherDelegate>(twAddress, UpdateTerritoryWeatherDetour);

            var skyTexAddress = DalamudServices.SigScanner.ScanText(XIVSigs.environmentSkybox);
            SkyTexHook = DalamudServices.GameInteropProvider.HookFromAddress<SkyTexDelegate>(skyTexAddress, SkyTexDetour);

            resetSky();

            UpdateWeathersForCurrentTerritory();

            DalamudServices.clientState.TerritoryChanged += OnTerritoryChanged;

        }

        public unsafe void resetSky()
        {
            SkyTexHook.Disable();
            var env = (ExtendedEnviornmentManager*)EnvManager.Instance();
            if (env == null) return;

            DalamudServices.framework.RunOnTick(()=>{ NewSkyID = env->SkyId; },TimeSpan.FromSeconds(.1));
            
        }

        private void OnTerritoryChanged(ushort e)
        {
            UpdateWeathersForCurrentTerritory();
            SkyTexHook.Disable();
            originalSkyID = CurrentSky;
            WeatherOverrideEnabled = false;
        }

        private unsafe void UpdateWeathersForCurrentTerritory()
        {
            _territoryWeatherTable.Clear();

            var envManager = EnvManager.Instance();

            if (envManager == null)
                return;

            var scenePtr = (nint)envManager->EnvScene;
            if (scenePtr == 0)
                return;

            byte* weatherIds = (byte*)(scenePtr + 0x2C);

            for (int i = 0; i < 32; ++i)
            {
                var weatherId = weatherIds[i];
                if (weatherId == 0)
                    continue;

                if (!GameResourceManager.Instance.Weathers.TryGetValue((uint)weatherId, out var weather))
                    continue;

                if (!_territoryWeatherTable.Any(x => x.RowId == weather.RowId))
                {
                    _territoryWeatherTable.Add(weather);
                }
            }

            _territoryWeatherTable.Sort((a, b) => a.RowId.CompareTo(b.RowId));
        }

        private void UpdateEorzeaTime(IntPtr a1, IntPtr a2)
        {
            // DO NOTHING
            // UpdateEorzeaTimeHook.Original(a1, a2);
        }

        private void UpdateTerritoryWeatherDetour(IntPtr a1, IntPtr a2)
        {
            // DO NOTHING
            //_updateTerritoryWeatherHook.Original(a1, a2);
        }

        private unsafe nint SkyTexDetour(nint a1, nint a2)
        {
            var res = SkyTexHook.Original(a1, a2);

            *(uint*)(a1 + 8) = NewSkyID;

            return res;
        }

        public void Dispose()
        {
            _updateEorzeaTimeHook.Dispose();
            _updateTerritoryWeatherHook.Dispose();
            SkyTexHook.Dispose();

            DalamudServices.clientState.TerritoryChanged -= OnTerritoryChanged;
        }
    }
}
