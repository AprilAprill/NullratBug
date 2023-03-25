﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RegionKit.Modules.Effects;
using static RegionKit.Modules.DevUIMisc.SettingsSaveOptions;
using DevInterface;
using System.Diagnostics;
using RegionKit.Modules.BackgroundBuilder;

namespace RegionKit.Modules.DevUIMisc;


[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "DevUI")]
public static class _Module
{
	private static bool __appliedOnce = false;
	internal static void Enable()
	{
		if (!__appliedOnce)
		{
			//what is this for
		}
		__appliedOnce = true;
		PaletteTextInput.Apply();
		CloudBuilder.Apply();

		//currently used for settings saving options stuffs, but will probably later be used for much more
		On.DevInterface.Page.ctor += Page_ctor;
		On.DevInterface.Page.Refresh += Page_Refresh;

		On.DevInterface.ObjectsPage.Signal += SubPage_Signal<On.DevInterface.ObjectsPage.orig_Signal, ObjectsPage>;
		On.DevInterface.RoomSettingsPage.Signal += SubPage_Signal<On.DevInterface.RoomSettingsPage.orig_Signal, RoomSettingsPage>;
		On.DevInterface.SoundPage.Signal += SubPage_Signal<On.DevInterface.SoundPage.orig_Signal, SoundPage>;
		On.DevInterface.TriggersPage.Signal += SubPage_Signal<On.DevInterface.TriggersPage.orig_Signal, TriggersPage>;
	}
	internal static void Disable()
	{
		PaletteTextInput.Undo();

		On.DevInterface.Page.ctor -= Page_ctor;
		On.DevInterface.Page.Refresh -= Page_Refresh;

		On.DevInterface.ObjectsPage.Signal -= SubPage_Signal<On.DevInterface.ObjectsPage.orig_Signal, ObjectsPage>;
		On.DevInterface.RoomSettingsPage.Signal -= SubPage_Signal<On.DevInterface.RoomSettingsPage.orig_Signal, RoomSettingsPage>;
		On.DevInterface.SoundPage.Signal -= SubPage_Signal<On.DevInterface.SoundPage.orig_Signal, SoundPage>;
		On.DevInterface.TriggersPage.Signal -= SubPage_Signal<On.DevInterface.TriggersPage.orig_Signal, TriggersPage>;
	}


	private static void Page_Refresh(On.DevInterface.Page.orig_Refresh orig, Page self)
	{
		//modSelectPanel = null;
		orig(self);
	}

	private static void SubPage_Signal<T, U>(T orig, U self, DevUISignalType type, DevUINode sender, string message)
		where T : Delegate
	{
		orig.DynamicInvoke(self, type, sender, message);
		if (self as Page is Page page)
		{ SaveSignal(page, type, sender, message); }
	}

	private static void Page_ctor(On.DevInterface.Page.orig_ctor orig, Page self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		//move pages over to avoid collision with save buttons
		//for BackgroundBuilder, currently unused
		foreach (DevUINode node in self.subNodes)
		{
			if (node is SwitchPageButton switchPageButton)
			{
				switchPageButton.pos.x -= 20f;
				switchPageButton.Refresh();
			}
		}

		if (self is MapPage or BuilderPage.BackgroundPage)
		{ settingsSaveOptionsMenu = null; return; }

		settingsSaveOptionsMenu = new SettingsSaveOptionsMenu(owner, "SettingsSaveOptions", self);
		self.subNodes.Add(settingsSaveOptionsMenu);
	}
}
