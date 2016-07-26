using System;
using System.Collections.Generic;

namespace War3Trainer
{
    #region Node type

    //////////////////////////////////////////////////////////////////////////
    // Function node
    enum TrainerNodeType
    {
        None,
        Root,

        Introduction,
        Cash,
        AllSelectedUnits,
        OneSelectedUnit,
        AttackAttributes,
        HeroAttributes,
        //UnitAbility,  // Not implemented yet
        AllItems,
        OneItem,
    }

    internal class NewChildrenEventArgs
        : EventArgs
    {
        public TrainerNodeType NodeType { get; set; }
        public int ParentNodeIndex      { get; set; }

        public UInt32 ThisGameAddress         { get; set; }   // [6FAA4178]
        public UInt32 ThisGameMemoryAddress   { get; set; }   // [ThisGame + 0xC]
        public UInt32 ThisUnitAddress         { get; set; }   // Unit ESI
        public UInt32 AttackAttributesAddress { get; set; }   // [ThisUnit + 1E4]
        public UInt32 HeroAttributesAddress   { get; set; }   // [ThisUnit + 1EC]
        public UInt32 CurrentItemPackAddress  { get; set; }   // [ThisUnit + 1F4]...

        public NewChildrenEventArgs()
        {
        }

        public NewChildrenEventArgs Clone()
        {
            NewChildrenEventArgs retObject = new NewChildrenEventArgs();

            retObject.NodeType                = this.NodeType;
            retObject.ParentNodeIndex         = this.ParentNodeIndex;
            retObject.ThisGameAddress         = this.ThisGameAddress;
            retObject.ThisGameMemoryAddress   = this.ThisGameMemoryAddress;
            retObject.ThisUnitAddress         = this.ThisUnitAddress;
            retObject.AttackAttributesAddress = this.AttackAttributesAddress;
            retObject.HeroAttributesAddress   = this.HeroAttributesAddress;
            retObject.CurrentItemPackAddress  = this.CurrentItemPackAddress;

            return retObject;
        }
    }
    
    internal interface ITrainerNode
    {
        TrainerNodeType NodeType { get; }
        string NodeTypeName { get; }
        int NodeIndex { get; }
        int ParentIndex { get; }
    }
    
    //////////////////////////////////////////////////////////////////////////
    // Address list node
    internal enum AddressListValueType
    {
        Integer,
        Float,
        Char4
    }

    internal class NewAddressListEventArgs
        : EventArgs
    {
        public int ParentNodeIndex { get; private set; }
        public string Caption { get; private set; }

        public UInt32 Address { get; private set; }
        public AddressListValueType ValueType { get; private set; }
        public int ValueScale { get; private set; }

        public NewAddressListEventArgs(
            int parentNodeIndex,
            string caption,
            UInt32 address,
            AddressListValueType valueType)
            : this(parentNodeIndex, caption, address, valueType, 1)
        {
        }

        public NewAddressListEventArgs(
            int parentNodeIndex,
            string caption,
            UInt32 address,
            AddressListValueType valueType,
            int valueScale)
        {
            this.ParentNodeIndex = parentNodeIndex;
            this.Caption = caption;

            this.Address = address;
            this.ValueType = valueType;
            this.ValueScale = valueScale;
        }
    }

    internal interface IAddressNode
    {
        int                  ParentIndex { get; }

        string               Caption     { get; }
        UInt32               Address     { get; }
        AddressListValueType ValueType   { get; }
        int                  ValueScale  { get; }
    }

    #endregion

    #region Basic nodes

    internal class TrainerNode
    {
        public event EventHandler<NewChildrenEventArgs> NewChildren;
        public event EventHandler<NewAddressListEventArgs> NewAddress;

        public virtual void CreateChildren()
        {
        }

        protected void CreateChild(
            TrainerNodeType childType,
            int parentIndex)
        {
            if (NewChildren != null)
            {
                NewChildrenEventArgs args = _newChildrenArgs.Clone();
                args.NodeType = childType;
                args.ParentNodeIndex = parentIndex;
                NewChildren(this, args);
            }
        }

        protected void CreateAddress(NewAddressListEventArgs addressListArgs)
        {
            if (NewAddress != null)
                NewAddress(this, addressListArgs);
        }

        protected int _nodeIndex;
        protected int _parentIndex;
        protected GameContext _gameContext;
        protected NewChildrenEventArgs _newChildrenArgs;

        public TrainerNode(
            int nodeIndex,
            GameContext gameContext,
            NewChildrenEventArgs args)
        {
            _nodeIndex = nodeIndex;
            _parentIndex = args.ParentNodeIndex;
            _gameContext = gameContext;
            _newChildrenArgs = args;
        }
    }

    class NodeAddressList
        : IAddressNode
    {
        protected NewAddressListEventArgs _AddresInfo;

        public NodeAddressList(NewAddressListEventArgs args)
        {
            _AddresInfo = args;
        }

        public int ParentIndex { get { return _AddresInfo.ParentNodeIndex; } }
        public string Caption { get { return _AddresInfo.Caption; } }
        public UInt32 Address { get { return _AddresInfo.Address; } }
        public AddressListValueType ValueType { get { return _AddresInfo.ValueType; } }
        public int ValueScale { get { return _AddresInfo.ValueScale; } }
    }

    #endregion

    #region Main collection
    
    //////////////////////////////////////////////////////////////////////////    
    // To build a node tree, and create objects using factory pattern
    class GameTrainer
    {
        private List<TrainerNode> _allTrainers = new List<TrainerNode>();
        private List<NodeAddressList> _allAdress = new List<NodeAddressList>();
        
        private GameContext _gameContext;

        #region Enumerator & Index

        public IEnumerable<ITrainerNode> GetFunctionList()
        {
            lock (_allTrainers)
            {
                foreach (var i in _allTrainers)
                    yield return i as ITrainerNode;
            }
        }

        public IEnumerable<IAddressNode> GetAddressList()
        {
            lock (_allAdress)
            {
                foreach (var i in _allAdress)
                    yield return i;
            }
        }

        #endregion

        // ctor()
        public GameTrainer(GameContext gameContext)
        {
            this._gameContext = gameContext;

            // Get trainers in 1st layer
            NewChildrenEventArgs args = new NewChildrenEventArgs();
            args.NodeType = TrainerNodeType.Root;
            NewChildrenEventReaction(this, args);

            // Get all other trainers
            int index = 0;
            int count = 1;
            while (index < count)
            {
                lock (_allTrainers)
                {
                    _allTrainers[index].CreateChildren();
                    index++;
                    count = _allTrainers.Count;
                }
            }
        }

        private void NewChildrenEventReaction(object sender, NewChildrenEventArgs e)
        {
            lock (_allTrainers)
            {
                TrainerNode newNode;
                int newNodeIndex = _allTrainers.Count;

                // Factory pattern                
                switch (e.NodeType)
                {
                    case TrainerNodeType.Root:
                        newNode = new RootNode(newNodeIndex, _gameContext, e);
                        break;
                
                    
                    case TrainerNodeType.AllSelectedUnits:
                        newNode = new AllSelectedUnitsNode(newNodeIndex, _gameContext, e);
                        break;
                    case TrainerNodeType.OneSelectedUnit:
                        newNode = new OneSelectedUnitNode(newNodeIndex, _gameContext, e);
                        break;
                   
                        throw new System.ArgumentException("e.NodeType");
                }
              
               
            }
        }

        private void NewAddressEventReaction(object sender, NewAddressListEventArgs e)
        {
            lock (_allAdress)
            {
                _allAdress.Add(new NodeAddressList(e));
            }
        }
    }

    #endregion

    #region Every functions
    
    //////////////////////////////////////////////////////////////////////////
    // Concrete trainer
    class RootNode
        : TrainerNode, ITrainerNode
    {
        public TrainerNodeType NodeType { get { return TrainerNodeType.Root; } }
        public string NodeTypeName { get { return "所有功能"; } }

        public int NodeIndex { get { return _nodeIndex; } }
        public int ParentIndex { get { return _parentIndex; } }

        public RootNode(int nodeIndex, GameContext gameContext, NewChildrenEventArgs args)
            : base(nodeIndex, gameContext, args)
        {
        }

        public override void CreateChildren()
        {
            // This function will fill the following value(s):
            //      _newChildrenArgs.ThisGame
            //      _newChildrenArgs.ThisGameMemory
            War3Common.GetGameMemory(
                _gameContext,
                ref _newChildrenArgs);
            
            base.CreateChild(TrainerNodeType.Introduction, NodeIndex);
            base.CreateChild(TrainerNodeType.Cash, NodeIndex);
            base.CreateChild(TrainerNodeType.AllSelectedUnits, NodeIndex);
        }
    }

 

    class AllSelectedUnitsNode
        : TrainerNode, ITrainerNode
    {
        public TrainerNodeType NodeType { get { return TrainerNodeType.AllSelectedUnits; } }
        public string NodeTypeName { get { return "选中单位列表"; } }

        public int NodeIndex { get { return _nodeIndex; } }
        public int ParentIndex { get { return _parentIndex; } }
        
        public AllSelectedUnitsNode(int nodeIndex, GameContext gameContext, NewChildrenEventArgs args)
            : base(nodeIndex, gameContext, args)
        {
        }

        public override void CreateChildren()
        {
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_gameContext.ProcessId))
            {
                // Get ESI of each selected unit
                UInt32 selectedUnitList = mem.ReadUInt32((IntPtr)_gameContext.UnitListAddress);
                UInt16 a2 = mem.ReadUInt16((IntPtr)unchecked(selectedUnitList + 0x28));
                UInt32 tmpAddress;
                tmpAddress = mem.ReadUInt32((IntPtr)unchecked(selectedUnitList + 0x58 + 4 * a2));
                tmpAddress = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x34));

                UInt32 listHead   = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x1F0));
                // UInt32 listEnd = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x1F4));
                UInt32 listLength = mem.ReadUInt32((IntPtr)unchecked(tmpAddress + 0x1F8));

                UInt32 nextNode = listHead;
                // UInt32 nextNodeNot = ~listHead;
                for (int selectedUnitIndex = 0; selectedUnitIndex < listLength; selectedUnitIndex++)
                {
                    _newChildrenArgs.ThisUnitAddress = mem.ReadUInt32((IntPtr)unchecked(nextNode + 8));
                    // nextNodeNot             = mem.ReadUInt32((IntPtr)unchecked(NextNode + 4));
                    nextNode                   = mem.ReadUInt32((IntPtr)unchecked(nextNode + 0));

                    base.CreateChild(TrainerNodeType.OneSelectedUnit, NodeIndex);
                }
            }
        }
    }

    #region 单位信息

    class OneSelectedUnitNode
        : TrainerNode, ITrainerNode
    {
        public TrainerNodeType NodeType { get { return TrainerNodeType.OneSelectedUnit; } }
        public string NodeTypeName
        {
            get
            {
                // return "单位";
                return "0x"
                    + _newChildrenArgs.ThisUnitAddress.ToString("X")
                    + ": "
                    + GetUnitName();
            }
        }

        public int NodeIndex { get { return _nodeIndex; } }
        public int ParentIndex { get { return _parentIndex; } }
        
        public OneSelectedUnitNode(int nodeIndex, GameContext gameContext, NewChildrenEventArgs args)
            : base(nodeIndex, gameContext, args)
        {
        }

        public string GetUnitName()
        {
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_gameContext.ProcessId))
            {
                return mem.ReadChar4((IntPtr)unchecked(_newChildrenArgs.ThisUnitAddress + 0x30));
            }
        }

        public override void CreateChildren()
        {
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_gameContext.ProcessId))
            {
                _newChildrenArgs.AttackAttributesAddress = mem.ReadUInt32((IntPtr)unchecked(_newChildrenArgs.ThisUnitAddress + _gameContext.AttackAttributesOffset));
                _newChildrenArgs.HeroAttributesAddress = mem.ReadUInt32((IntPtr)unchecked(_newChildrenArgs.ThisUnitAddress + _gameContext.HeroAttributesOffset));

                if (_newChildrenArgs.AttackAttributesAddress > 0)
                {
                    base.CreateChild(TrainerNodeType.AttackAttributes, NodeIndex);
                    // base.CreateChild(TrainerNodeType.UnitAbility, NodeIndex);
                    base.CreateChild(TrainerNodeType.AllItems, NodeIndex);
                }

                if (_newChildrenArgs.HeroAttributesAddress > 0)
                {
                    base.CreateChild(TrainerNodeType.HeroAttributes, NodeIndex);
                }

                // Unit self propety(s)
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "单位名称",
                    unchecked(_newChildrenArgs.ThisUnitAddress + 0x30),
                    AddressListValueType.Char4));

                UInt32 tmpAddress1, tmpAddress2;
                Int32 tmpValue1, tmpValue2;
                tmpValue1 = mem.ReadInt32((IntPtr)unchecked(_newChildrenArgs.ThisUnitAddress + 0x98 + 0x8));
                tmpAddress1 = War3Common.ReadFromGameMemory(
                    mem, _gameContext, _newChildrenArgs,
                    tmpValue1);
                tmpAddress1 = unchecked(tmpAddress1 + 0x84);
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "HP - 目前",
                    unchecked(tmpAddress1 - 0xC),
                    AddressListValueType.Float));
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "HP - 最大",
                    tmpAddress1,
                    AddressListValueType.Float));
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "HP - 回复率",
                    unchecked(_newChildrenArgs.ThisUnitAddress + 0xB0),
                    AddressListValueType.Float));

                tmpValue1 = mem.ReadInt32((IntPtr)unchecked(_newChildrenArgs.ThisUnitAddress + 0x98 + 0x28));
                tmpAddress1 = War3Common.ReadFromGameMemory(
                    mem, _gameContext, _newChildrenArgs,
                    tmpValue1);
                tmpAddress1 = unchecked(tmpAddress1 + 0x84);
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "MP - 目前",
                    unchecked(tmpAddress1 - 0xC),
                    AddressListValueType.Float));
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "MP - 最大",
                    tmpAddress1,
                    AddressListValueType.Float));
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                   "MP - 回复率",
                   unchecked(_newChildrenArgs.ThisUnitAddress + 0xD4),
                   AddressListValueType.Float));
                
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "盔甲 - 数量",
                    unchecked(_newChildrenArgs.ThisUnitAddress + 0xE0),
                    AddressListValueType.Float));
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "盔甲 - 种类",
                    unchecked(_newChildrenArgs.ThisUnitAddress + 0xE4),
                    AddressListValueType.Integer));
                
                // Move speed
                tmpAddress1 = unchecked(_newChildrenArgs.ThisUnitAddress + _gameContext.MoveSpeedOffset - 0x24);
                do
                {
                    tmpValue1 = mem.ReadInt32((IntPtr)unchecked(tmpAddress1 + 0x24));
                    tmpAddress1 = War3Common.ReadGameValue2(
                        mem, _gameContext, _newChildrenArgs,
                        tmpValue1);
                    tmpAddress2 = mem.ReadUInt32((IntPtr)unchecked(tmpAddress1 + 0));
                    tmpValue1 = mem.ReadInt32((IntPtr)unchecked(tmpAddress1 + 0x24));
                    tmpValue2 = mem.ReadInt32((IntPtr)unchecked(tmpAddress1 + 0x28));

                    // Note: If new game version released, set breakpoint here
                    //       and check tmpAddress2. Set this value as War3AddressMoveSpeed
                    tmpAddress2 = mem.ReadUInt32((IntPtr)unchecked(tmpAddress2 + 0x2D4));
                    if (_gameContext.MoveSpeedAddress == tmpAddress2)
                    {
                        CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                            "移动速度",
                            unchecked(tmpAddress1 + 0x70),  // +70 or +78 are both OK
                            AddressListValueType.Float));
                    }
                } while (tmpValue1 > 0 && tmpValue2 > 0);

                // Coordinate
                tmpValue1 = mem.ReadInt32((IntPtr)unchecked(_newChildrenArgs.ThisUnitAddress + 0x164 + 8));
                tmpAddress1 = War3Common.ReadGameValue1(
                    mem, _gameContext, _newChildrenArgs,
                    tmpValue1);
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "坐标 - X",
                    tmpAddress1,
                    AddressListValueType.Float));
                CreateAddress(new NewAddressListEventArgs(_nodeIndex,
                    "坐标 - Y",
                    unchecked(tmpAddress1 + 4),
                    AddressListValueType.Float));
            }
        }
    }

    #endregion 

    
    #endregion
}
