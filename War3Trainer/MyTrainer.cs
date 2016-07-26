using System;
using System.Collections.Generic;
using System.Text;

namespace War3Trainer
{
    public class MyTrainer
    {
        private GameContext _currentGameContext;
        private NewChildrenEventArgs _currentArgs;
        public MyTrainer()
        {
            FindGame();
        }
        public void GetInfo()
        {
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
            {
                UInt32 tmpAddress1;
                Int32 tmpValue1;
                tmpValue1 = mem.ReadInt32((IntPtr)unchecked(_currentArgs.ThisUnitAddress + 0x98 + 0x8));
                tmpAddress1 = War3Common.ReadFromGameMemory(
                    mem, _currentGameContext, _currentArgs,
                    tmpValue1);
                tmpAddress1 = unchecked(tmpAddress1 + 0x84);
                object itemValue = mem.ReadFloat((IntPtr)unchecked(tmpAddress1 - 0xC));

                string hp = itemValue.ToString();
            }
        }
        public void GetInfo( HeroInfo info)
        {
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
            {
                UInt32 tmpAddress1;
                Int32 tmpValue1;
                tmpValue1 = mem.ReadInt32((IntPtr)unchecked(info.Address + 0x98 + 0x8));
                tmpAddress1 = War3Common.ReadFromGameMemory(
                    mem, _currentGameContext, _currentArgs,
                    tmpValue1);
                tmpAddress1 = unchecked(tmpAddress1 + 0x84);
                object itemValue = mem.ReadFloat((IntPtr)unchecked(tmpAddress1 - 0xC));
                info.HP = (int)Convert.ToDouble(itemValue.ToString());

                // Coordinate
                tmpValue1 = mem.ReadInt32((IntPtr)unchecked(info.Address + 0x164 + 8));
                tmpAddress1 = War3Common.ReadGameValue1(
                    mem, _currentGameContext, _currentArgs,
                    tmpValue1);
   
                itemValue = mem.ReadFloat((IntPtr)unchecked(tmpAddress1));
                info.X = (int)Convert.ToDouble(itemValue.ToString());

                itemValue = mem.ReadFloat((IntPtr)unchecked(tmpAddress1 + 4));
                info.Y = (int)Convert.ToDouble((itemValue.ToString()));


                itemValue = mem.ReadChar4((IntPtr)unchecked(info.Address + 0x30));
         
               info.Name = itemValue.ToString();
            }
        }
        public void FindGame()
        {
            _currentGameContext = GameContext.FindGameRunning("war3", "game.dll");
            if (_currentGameContext==null)
            {
                return;
            }
            _currentArgs = new NewChildrenEventArgs();
            War3Common.GetGameMemory(_currentGameContext, ref _currentArgs);
        }

        public void GetCurrentHeroAddress(ref HeroInfo info)
        {
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
            {
                // Get ESI of each selected unit
                UInt32 selectedUnitList = mem.ReadUInt32((IntPtr)_currentGameContext.UnitListAddress);
                UInt16 a2 = mem.ReadUInt16((IntPtr)unchecked(selectedUnitList + 0x28));
                UInt32 tmpAddress;
                tmpAddress = mem.ReadUInt32((IntPtr)unchecked(selectedUnitList + 0x58 + 4 * a2));
                tmpAddress = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x34));

                UInt32 listHead = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x1F0));
                // UInt32 listEnd = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x1F4));
                UInt32 listLength = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x1F8));

                UInt32 nextNode = listHead;
                info.Address = mem.ReadUInt32((IntPtr)unchecked(nextNode + 8));
            }
        }
    }
}
