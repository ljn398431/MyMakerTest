using BloxEngine;
using System.Collections.Generic;

namespace BloxEditor
{
	public class BloxEventEd
	{
		public BloxEvent ev;

		public BloxEventDef def;

		public BloxBlockEd firstBlock;

		public List<BloxBlockEd> unlinkedBlocks = new List<BloxBlockEd>();

		public bool hasUndefinedblocks;

		public void Clear()
		{
			this.ev = null;
			this.def = null;
			this.firstBlock = null;
			this.hasUndefinedblocks = false;
			this.unlinkedBlocks.Clear();
		}

		public void Set(BloxEvent ev, bool loadUnlinkedBlocks = true)
		{
			this.hasUndefinedblocks = false;
			if (this.ev != ev)
			{
				this.Clear();
				if (ev != null)
				{
					this.def = BloxEd.Instance.FindEventDef(ev);
					if (this.def != null)
					{
						this.ev = ev;
						this.firstBlock = ((ev.firstBlock == null) ? null : new BloxBlockEd(ev.firstBlock, null, null, null, -1));
						if (loadUnlinkedBlocks)
						{
							for (int i = 0; i < ev.unlinkedBlocks.Count; i++)
							{
								this.unlinkedBlocks.Add(new BloxBlockEd(ev.unlinkedBlocks[i], null, null, null, -1));
							}
						}
					}
				}
			}
		}

		public bool HasYieldInstruction()
		{
			return this._CheckForYieldRecursive(this.firstBlock);
		}

		private bool _CheckForYieldRecursive(BloxBlockEd b)
		{
			while (b != null)
			{
				if (b.def.isYieldBlock)
				{
					return true;
				}
				if (b.firstChild != null && this._CheckForYieldRecursive(b.firstChild))
				{
					return true;
				}
				b = b.next;
			}
			return false;
		}

		public bool CheckEventBlockDefs()
		{
			this.hasUndefinedblocks = false;
			if (this.firstBlock == null)
			{
				return true;
			}
			if (BloxEd.Instance.BlockDefsLoading)
			{
				BloxEd.Instance.DoUpdate();
				return false;
			}
			if (BloxBlocksList.HasInstance && BloxBlocksList.Instance.IsBuildingList)
			{
				BloxBlocksList.Instance.DoUpdate();
				return false;
			}
			BloxBlockEd next = this.firstBlock;
			while (next != null && !this.CheckBlockDef(next))
			{
				next = next.next;
			}
			return true;
		}

		private bool CheckBlockDef(BloxBlockEd bdi)
		{
			BloxBlock _ = bdi.b;
			if (bdi.b.blockType != 0 && (bdi.b.mi == null || bdi.b.mi.IsValid))
			{
				if (bdi.def == null)
				{
					bdi.def = BloxEd.Instance.FindBlockDef(bdi.b);
					if (((bdi.def != null) ? bdi.def.blockType : BloxBlockType.Unknown) != 0)
					{
						goto IL_0078;
					}
					this.hasUndefinedblocks = true;
					return true;
				}
				goto IL_0078;
			}
			this.hasUndefinedblocks = true;
			return true;
			IL_0078:
			if (bdi.contextBlock != null && this.CheckBlockDef(bdi.contextBlock))
			{
				return true;
			}
			for (int i = 0; i < bdi.paramBlocks.Length; i++)
			{
				if (bdi.paramBlocks[i] != null && this.CheckBlockDef(bdi.paramBlocks[i]))
				{
					return true;
				}
			}
			for (bdi = bdi.firstChild; bdi != null; bdi = bdi.next)
			{
				if (this.CheckBlockDef(bdi))
				{
					return true;
				}
			}
			return false;
		}
	}
}
