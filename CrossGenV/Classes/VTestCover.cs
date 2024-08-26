using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Handles cover-related things.
    /// </summary>
    public class VTestCover
    {
        private static StructProperty ConvertCoverSlot(StructProperty me1CoverSlotProps, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // How to convert a coverslot

            // 1. Draw some circles
            var csProps = GlobalUnrealObjectInfo.getDefaultStructValue(MEGame.LE1, "CoverSlot", true, le1File, vTestOptions.cache);

            // Populate Actions in 684 before we enumerate things
            var actions = csProps.GetProp<ArrayProperty<EnumProperty>>("Actions");
            actions.Clear();
            if (me1CoverSlotProps.GetProp<BoolProperty>("bLeanLeft").Value) actions.Add(new EnumProperty("CA_LeanLeft", "ECoverAction", MEGame.LE1));
            if (me1CoverSlotProps.GetProp<BoolProperty>("bLeanRight").Value) actions.Add(new EnumProperty("CA_LeanRight", "ECoverAction", MEGame.LE1));
            if (me1CoverSlotProps.GetProp<BoolProperty>("bCanPopUp").Value) actions.Add(new EnumProperty("CA_PopUp", "ECoverAction", MEGame.LE1));
            // Might be more but no clue.


            // 2. Draw the rest of the fucking owl
            foreach (var me1Prop in me1CoverSlotProps.Properties.ToList())
            {
                switch (me1Prop)
                {
                    case IntProperty:
                    case FloatProperty:
                    case BoolProperty:
                    case EnumProperty:
                        if (TryUpdateProp(me1Prop, csProps))
                        {
                            me1CoverSlotProps.Properties.Remove(me1Prop);
                        }

                        break;
                    case ObjectProperty op:
                        if (op.Value == 0)
                            me1CoverSlotProps.Properties.Remove(me1Prop); // This doesn't have a value
                        break;
                    case StructProperty sp:
                        {

                            if (sp.Name == "LocationOffset" || sp.Name == "RotationOffset")
                            {
                                // These can be directly moved.
                                if (!sp.IsImmutable)
                                    Debugger.Break();
                                TryUpdateProp(me1Prop, csProps);
                                me1CoverSlotProps.Properties.Remove(sp);
                            }

                            if (sp.Name == "MantleTarget")
                            {
                                ConvertCoverReference(sp, csProps.GetProp<StructProperty>("MantleTarget").Properties, me1File, le1File, vTestOptions);
                                me1CoverSlotProps.Properties.Remove(sp);
                            }

                            break;
                        }
                    case ArrayProperty<StructProperty> asp:
                        {
                            switch (asp.Name)
                            {
                                case "DangerLinks":
                                    {
                                        var le1DLProp = csProps.GetProp<ArrayProperty<StructProperty>>("DangerLinks");
                                        foreach (var dl in asp)
                                        {
                                            var dlProps = GlobalUnrealObjectInfo.getDefaultStructValue(MEGame.LE1, "DangerLink", true, le1File, vTestOptions.cache);
                                            ConvertDangerLink(dl, dlProps, me1File, le1File, vTestOptions);
                                            le1DLProp.Add(new StructProperty("DangerLink", dlProps, isImmutable: true));
                                        }

                                        break;
                                    }
                                case "ExposedFireLinks":
                                    {
                                        var le1DLProp = csProps.GetProp<ArrayProperty<StructProperty>>("ExposedFireLinks");
                                        if (le1DLProp.Count > 0)
                                            Debugger.Break(); // This should be empty to start with...
                                        int linkNum = 0;
                                        foreach (var dl in asp)
                                        {
                                            // CoverReference -> ExposedLink (ExposedScale). No way to compute this at all... Guess just random ¯\_(ツ)_/¯
                                            var dlProps = GlobalUnrealObjectInfo.getDefaultStructValue(MEGame.LE1, "ExposedLink", true, le1File, vTestOptions.cache);
                                            ConvertExposedLink(dl, dlProps, me1File, le1File, vTestOptions);
                                            le1DLProp.Add(new StructProperty("ExposedLink", dlProps, isImmutable: true));
                                            //Debug.WriteLine($"Converted EFL {linkNum} of {asp.Count}");
                                            linkNum++;
                                        }

                                        break;
                                    }
                                case "FireLinks":
                                case "ForcedFireLinks":
                                    {
                                        var le1DLProp = csProps.GetProp<ArrayProperty<StructProperty>>(asp.Name);
                                        foreach (var dl in asp)
                                        {
                                            // FireLink -> FireLink. This struct changed a lot
                                            var dlProps = GlobalUnrealObjectInfo.getDefaultStructValue(MEGame.LE1, "FireLink", true, le1File, vTestOptions.cache);
                                            ConvertFireLink(dl, dlProps, me1File, le1File, vTestOptions);
                                            le1DLProp.Add(new StructProperty("FireLink", dlProps, isImmutable: true));
                                        }

                                        break;
                                    }
                                case "OverlapClaims":
                                case "TurnTarget":
                                    {
                                        var le1DLProp = csProps.GetProp<ArrayProperty<StructProperty>>(asp.Name);
                                        foreach (var me1CovRef in asp)
                                        {
                                            // FireLink -> FireLink. This struct changed a lot
                                            var le1CovRefProps = GlobalUnrealObjectInfo.getDefaultStructValue(MEGame.LE1, me1CovRef.StructType, true, le1File, vTestOptions.cache);
                                            ConvertCoverReference(me1CovRef, le1CovRefProps, me1File, le1File, vTestOptions);
                                            le1DLProp.Add(new StructProperty(me1CovRef.StructType, le1CovRefProps, isImmutable: true));
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }

            if (me1CoverSlotProps.Properties.Count > 0)
            {
                // uncomment to debug these
                //Debug.WriteLine("The following properties were not translated:");
                foreach (var mp in me1CoverSlotProps.Properties)
                {
                    //Debug.WriteLine(mp.Name );
                }
            }


            return new StructProperty("CoverSlot", csProps, isImmutable: true);

            bool TryUpdateProp(Property p, PropertyCollection destCollection)
            {
                if (destCollection.ContainsNamedProp(p.Name))
                {
                    destCollection.AddOrReplaceProp(p);
                    return true;
                }
                Debug.WriteLine($"Target doesn't have property named {p.Name}");
                return false;
            }
        }

        private static void ConvertFireLink(StructProperty me1FL, PropertyCollection le1FL, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            le1FL.GetProp<BoolProperty>("bFallbackLink").Value = me1FL.GetProp<BoolProperty>("bFallbackLink")?.Value ?? false;
            var mta = me1FL.GetProp<StructProperty>("TargetLink");
            var slotIdx = me1FL.GetProp<IntProperty>("TargetSlotIdx");
            var lta = le1FL.GetProp<StructProperty>("TargetActor").Properties;
            ConvertNavRefToCoverRef(mta, lta, slotIdx, me1File, le1File, vTestOptions);

            // Items MUST BE DONE ON A SECOND PASS ONCE ALL THE COVERSLOTS HAVE BEEN GENERATED
        }

        public static void GenerateFireLinkItemsForFile(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // This appears to be map of SourceCoverType and the action on it -> Destination Cover Type and Action
            // E.g. This FirELinkItem is for Me popping up, shooting at a coverlink that has someone doing action Default MidLevel (hiding)
            // Will require reading the destination CoverSlots so this will actually probably need to be done on a second pass...

            foreach (var le1Cl in le1File.Exports.Where(x => x.ClassName == "CoverLink"))
            {
                var me1cl = me1File.FindExport(le1Cl.InstancedFullPath);

                var me1Props = me1cl.GetProperties();
                var le1Props = le1Cl.GetProperties();

                var me1Slots = me1Props.GetProp<ArrayProperty<StructProperty>>("Slots");
                var le1Slots = le1Props.GetProp<ArrayProperty<StructProperty>>("Slots");

                for (int i = 0; i < me1Slots.Count; i++)
                {
                    var me1Slot = me1Slots[i];
                    var le1Slot = le1Slots[i];
                    GenerateFireLinkItemsForSlot(me1Slot, le1Slot, me1File, le1File, vTestOptions);
                }

                le1Cl.WriteProperties(le1Props);
            }
        }

        private static void GenerateFireLinkItemsForSlot(StructProperty me1Slot, StructProperty le1Slot, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // GET THE RAGU MARIO
            // WE'RE A MAKIN SPAGHETTI

            var me1FireLinks = me1Slot.GetProp<ArrayProperty<StructProperty>>("FireLinks");
            var le1FireLinks = le1Slot.GetProp<ArrayProperty<StructProperty>>("FireLinks");
            for (int i = 0; i < me1FireLinks.Count; i++)
            {
                var le1FL = le1FireLinks[i];
                var le1Items = le1FL.GetProp<ArrayProperty<StructProperty>>("Items"); // We will populate this list

                var targetActor = le1FL.GetProp<StructProperty>("TargetActor");
                var destCoverVal = targetActor.GetProp<ObjectProperty>("Actor");
                if (destCoverVal == null)
                    Debugger.Break(); // it's cross level, what a nightmare

                var destSlotIdx = targetActor.GetProp<IntProperty>("SlotIdx");
                var destCover = destCoverVal.ResolveToEntry(le1File) as ExportEntry;
                var destSlot = destCover.GetProperty<ArrayProperty<StructProperty>>("Slots")[destSlotIdx];

                var destType = destSlot.GetProp<EnumProperty>("CoverType").Value; //DestType
                List<string> destActions = new List<string>();
                if (destSlot.GetProp<BoolProperty>("bLeanLeft")) destActions.Add("CA_LeanLeft");
                if (destSlot.GetProp<BoolProperty>("bLeanRight")) destActions.Add("CA_LeanRight");
                if (destSlot.GetProp<BoolProperty>("bCanPopUp")) destActions.Add("CA_PopUp");
                destActions.Add("CA_Default"); // This doesn't seem reliable but idk what else to do

                var srcType = me1FireLinks[i].GetProp<EnumProperty>("CoverType").Value;

                int generated = 0;
                var srcActions = me1FireLinks[i].GetProp<ArrayProperty<EnumProperty>>("CoverActions");
                foreach (var srcAction in srcActions)
                {
                    // This now has enough info for SrcType, SrcAction, destType in the dest

                    // UNKNOWN HOW THE DEST ACTION IS DETERMINED, IT DOESN'T APPEAR RELIABLE. See above
                    foreach (var destAction in destActions)
                    {
                        PropertyCollection fliProps = new PropertyCollection();
                        fliProps.Add(new EnumProperty(srcType, "ECoverType", MEGame.LE1, "SrcType"));
                        fliProps.Add(new EnumProperty(srcAction.Value, "ECoverAction", MEGame.LE1, "SrcAction"));
                        fliProps.Add(new EnumProperty(destType, "ECoverType", MEGame.LE1, "DestType"));
                        fliProps.Add(new EnumProperty(destAction, "ECoverAction", MEGame.LE1, "DestAction"));
                        le1Items.Add(new StructProperty("FireLinkItem", fliProps, isImmutable: true));
                        generated++;
                        //Debug.WriteLine($"Generated FLI {generated}. DAC: {destActions.Count}, SAC: {srcActions.Count}");
                    }
                }
            }

        }

        private static void ConvertNavRefToCoverRef(StructProperty mta, PropertyCollection lta, IntProperty slotIdx, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // ME1: NavReference (w/ External SlotIdx)
            // LE1: CoverReference
            // We don't bother changing the Direction.

            lta.GetProp<IntProperty>("SlotIdx").Value = slotIdx;
            lta.GetProp<StructProperty>("Guid").Properties = mta.GetProp<StructProperty>("Guid").Properties;

            var nav = mta.GetProp<ObjectProperty>("Nav");
            lta.GetProp<ObjectProperty>("Actor").Value = le1File.FindExport(me1File.GetUExport(nav.Value).InstancedFullPath).UIndex;

        }

        private static void ConvertExposedLink(StructProperty me1ELStruct, PropertyCollection le1ELStructProps, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // ME1: NavReference
            // LE1: DangerLink
            // We don't bother changing the DangerCost.
            var le1ExposedTargetActor = le1ELStructProps.GetProp<StructProperty>("TargetActor");
            ConvertCoverReference(me1ELStruct, le1ExposedTargetActor.Properties, me1File, le1File, vTestOptions);

            // The ExposedScale is the amount of exposure to other links. Higher exposure is better... I think?
            // This is computed during map cook so ... yeah ... ... ...
            le1ELStructProps.GetProp<ByteProperty>("ExposedScale").Value = 128; // No idea what to put here
        }

        private static void ConvertDangerLink(StructProperty me1DLStruct, PropertyCollection le1DLStruct, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // ME1: NavReference
            // LE1: DangerLink
            // We don't bother changing the DangerCost.
            var le1ARStruct = le1DLStruct.GetProp<StructProperty>("DangerNav");
            ConvertNavRefToActorRef(me1DLStruct, le1ARStruct.Properties, me1File, le1File, vTestOptions);
        }

        public static void ConvertNavRefToActorRef(StructProperty me1NRStruct, PropertyCollection le1ARStruct, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            le1ARStruct.GetProp<StructProperty>("Guid").Properties = me1NRStruct.GetProp<StructProperty>("Guid").Properties;
            var nav = me1NRStruct.GetProp<ObjectProperty>("Nav");
            if (nav.Value != 0)
            {
                // All navigation points should have been imported by VTest... Soo......
                // Hopefully this works
                le1ARStruct.GetProp<ObjectProperty>("Actor").Value = le1File.FindExport(me1File.GetUExport(nav.Value).InstancedFullPath).UIndex;
                //Debugger.Break();
            }
        }

        /// <summary>
        /// Converts CoverReference (491) -> CoverReference (684)
        /// </summary>
        /// <param name="me1Prop"></param>
        /// <param name="le1Props"></param>
        /// <param name="targetPropName"></param>
        /// <param name="me1File"></param>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        private static void ConvertCoverReference(StructProperty me1Prop, PropertyCollection le1Props, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            le1Props.GetProp<IntProperty>("SlotIdx").Value = me1Prop.GetProp<IntProperty>("SlotIdx").Value;
            le1Props.GetProp<IntProperty>("Direction").Value = me1Prop.GetProp<IntProperty>("Direction").Value;
            le1Props.GetProp<StructProperty>("Guid").Properties = me1Prop.GetProp<StructProperty>("Guid").Properties;
            var nav = me1Prop.GetProp<ObjectProperty>("Nav");
            if (nav.Value != 0)
            {
                // All navigation points should have been imported by VTest... Soo......
                // Hopefully this works
                le1Props.GetProp<ObjectProperty>("Actor").Value = le1File.FindExport(me1File.GetUExport(nav.Value).InstancedFullPath).UIndex;
                //Debugger.Break();
            }
            // Default is 0 so don't have to do anything
        }

        public static void ReinstateCoverSlots(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            var coverLinks = le1File.Exports.Where(x => x.ClassName == "CoverLink");
            foreach (var le1CoverLink in coverLinks)
            {
                var me1CoverLink = me1File.FindExport(le1CoverLink.InstancedFullPath);
                var me1Slots = me1CoverLink.GetProperty<ArrayProperty<StructProperty>>("Slots");
                if (me1Slots != null && me1Slots.Any())
                {
                    var le1Slots = new ArrayProperty<StructProperty>("Slots");

                    foreach (var slot in me1Slots)
                    {
                        le1Slots.Add(ConvertCoverSlot(slot, me1File, le1File, vTestOptions));
                    }

                    le1CoverLink.WriteProperty(le1Slots);
                    //le1File.Save();
                }
            }
        }
    }
}
