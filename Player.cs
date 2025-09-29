using BepInEx;
using BepInEx.Logging;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInExLogger = BepInEx.Logging;

namespace LevicksSeedTest;

public partial class LevicksSeedTest
{
    public static string newRegion = "";

    public static TestContainer warpContainer;
    public static List<ListItem> regs = new();
    public static List<string> mapList = new();

    private static new ManualLogSource Logger;

    private void SetInit(ManualLogSource logger)
    {
        Logger = logger;
        On.Menu.PauseMenu.ctor += PauseMenu_ctor;
    }

    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        orig.Invoke(self, manager, game);
        //SymbolButton button = new SymbolButton(self, self.pages[0], "Menu_Symbol_Show_List", "SETTINGS", new Vector2(self.exitButton.pos.x - 40f, self.exitButton.pos.y + 2f));
        //self.pages[0].subObjects.Add(button);

        warpContainer = new TestContainer(self, self.pages[0], self.pages[0].pos, new Vector2());
        self.pages[0].subObjects.Add(warpContainer);

    }

    private static List<ListItem> GetAbleTargets(RainWorldGame game)
    {
        int seed = game.rainWorld.progression.miscProgressionData.watcherCampaignSeed;

        AbstractRoom room = game.Players[0].Room;

        mapList = Watcher.WarpPoint.GetAvailableDynamicWarpTargets(room.world, room.name, null, false);

        List<string> allowLists = new();

        foreach (var content in mapList)
            if (!allowLists.Contains(content))
                allowLists.Add(content);

        List<ListItem> returnValue = new();

        for (int i = 0; i < allowLists.Count; i++)
        {
            returnValue.Add(new ListItem(allowLists[i], i));
            Logger.LogDebug(allowLists[i]);
        }

        return returnValue;
    }

    public class TestContainer : RectangularMenuObject
    {
        public RainWorldGame game;
        public MenuTabWrapper tabWrapper;
        public OpComboBox regionDropdown;
        public MenuLabel desc2;
        public string seedText;
        public Configurable<string> selectRoom;

        public int currentSeed = 0;
        public int createCount = 0;

        public TestContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            game = (menu as PauseMenu).game;

            currentSeed = game.rainWorld.progression.miscProgressionData.watcherCampaignSeed;
            createCount = game.GetStorySession.saveState.miscWorldSaveData.numberOfWarpPointsGenerated;

            regs = GetAbleTargets(game);
            GetSeedText(regs[0].name);

            selectRoom = new Configurable<string>(newRegion);

            MenuLabel desc1 = new MenuLabel(menu, this, "WarpAblePositions", new Vector2(10f, game.rainWorld.options.ScreenSize.y - 65f), new Vector2(), false);
            desc1.label.alignment = FLabelAlignment.Left;

            subObjects.Add(desc1);

            tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);


            regionDropdown = new OpComboBox(selectRoom, new Vector2(10f, desc1.pos.y - 40f), 150f, regs);
            regionDropdown.listHeight = 29;
            regionDropdown.OnValueChanged += RegionDropdown_OnValueChanged;
            UIelementWrapper wrapper = new UIelementWrapper(tabWrapper, regionDropdown);

            SimpleButton copyButton = new SimpleButton(menu, this, "SeedCopy", "Copy", new Vector2(200f, regionDropdown.PosY), new Vector2(100f, 30f));
            subObjects.Add(copyButton);

            desc2 = new MenuLabel(menu, this, $"currentTargetRoom = {GetCurrentRoom(game)}", new Vector2(10f, regionDropdown.PosY - 50), new Vector2(), false);
            desc2.label.alignment = FLabelAlignment.Left;
            subObjects.Add(desc2);
        }

        private void RegionDropdown_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            GetSeedText(value);
            desc2.text = $"currentTargetRoom = {GetCurrentRoom(game)}";
        }

        private string GetCurrentRoom(RainWorldGame game)
        {
            UnityEngine.Random.State wasState = UnityEngine.Random.state;


            UnityEngine.Random.InitState(currentSeed + createCount);
            string map = mapList[UnityEngine.Random.Range(0, mapList.Count)];


            UnityEngine.Random.state = wasState;
            return map;
        }

        private void GetSeedText(string value)
        {
            UnityEngine.Random.State wasState = UnityEngine.Random.state;

            var sb = new System.Text.StringBuilder();
            int count = 0;

            for (int i = 0; i < 100000; i++)
            {
                UnityEngine.Random.InitState(i);
                int index = UnityEngine.Random.Range(0, mapList.Count);

                if (value == mapList[index])
                {
                    count++;
                    sb.Append(i);
                    sb.Append(", ");

                    if (count % 20 == 0)
                    {
                        sb.Append("\r\n");
                    }
                }
            }

            seedText = sb.ToString();

            UnityEngine.Random.state = wasState;
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);

            if (message == "Copy")
            {
                Logger.LogMessage("copied seed");

                GUIUtility.systemCopyBuffer = seedText;
            }
        }
    }
}

