using rinCore;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace FumoShmup2
{
    public class EnemyModifierNode : StageNode
    {
        #region Draw Node Actions
#if UNITY_EDITOR
        public static int CountDrawFields(object target)
        {
            if (target == null)
                return 0;

            Type type = target.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            int count = 0;

            foreach (var field in fields)
            {
                if (field.IsNotSerialized) continue;
                if (Attribute.IsDefined(field, typeof(HideInInspector))) continue;

                count++;
            }

            return count;
        }
        public static void DrawActionFields(object target, Rect startRect)
        {
            if (target == null)
                return;

            float lineHeight = EditorGUIUtility.singleLineHeight + 2f;
            Rect fieldRect = startRect;

            Type type = target.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (field.IsNotSerialized) continue;
                if (Attribute.IsDefined(field, typeof(HideInInspector))) continue;

                object value = field.GetValue(target);
                Type fieldType = field.FieldType;

                if (fieldType == typeof(float))
                {
                    value = EditorGUI.FloatField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (float)(value ?? 0f));
                }
                else if (fieldType == typeof(int))
                {
                    value = EditorGUI.IntField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (int)(value ?? 0));
                }
                else if (fieldType == typeof(bool))
                {
                    value = EditorGUI.Toggle(fieldRect, ObjectNames.NicifyVariableName(field.Name), (bool)(value ?? false));
                }
                else if (fieldType.IsEnum)
                {
                    value = EditorGUI.EnumPopup(fieldRect, ObjectNames.NicifyVariableName(field.Name), (Enum)(value ?? Activator.CreateInstance(fieldType)));
                }
                else if (fieldType == typeof(string))
                {
                    value = EditorGUI.TextField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (string)(value ?? ""));
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                {
                    value = EditorGUI.ObjectField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (UnityEngine.Object)value, fieldType, true);
                }
                else if (!fieldType.IsPrimitive && fieldType.IsClass)
                {
                    EditorGUI.LabelField(fieldRect, ObjectNames.NicifyVariableName(field.Name), $"({fieldType.Name})");
                }
                field.SetValue(target, value);
                fieldRect.y += lineHeight;
            }
        }
#endif
        #endregion
        #region Fields
        public float EnemyHealth = 100f;
        public bool LargeHealthPool = false;
        public UnitAttack AttackOverride;
        public RevengeAttack RevengeAttackOverride;
        public bool Indicator;

        public bool AmmoOverride;
        public bool AmmoSecondsOrCount;
        public int ammoCount = 1;
        public float ammoTime = 1f;
        public float attackDelay = 0.5f;

        [SerializeReference]
        public NodeActions.BaseAction actionOverride;
        #endregion

        public List<StageNode> LinkedNodes = new();
        [NYI("Feature")]
        public void ModifyEnemy(EnemyUnit e)
        {
            bool hasPlayer = ShmupPlayer.PlayerAs(out ShmupPlayer p);
            if (actionOverride is NodeActions.BaseAction action)
            {
                action.StartAction(e);
            }
            e.StartNewHealth(EnemyHealth, EnemyHealth);
            if (RevengeAttackOverride != null) e.SetRevengeAttackOverride(new(RevengeAttackOverride));
            //e.SetAttackStall(attackDelay);
            if (Indicator)
            {
                //EnemyIndicator.TrackUnit(e);
            }
        }
        public void LinkNode(ShmupNodeStage stage, StageNode node)
        {
            foreach (var mods in stage.nodes.Where(n => n != null).OfType<EnemyModifierNode>())
            {
                mods.LinkedNodes.Remove(node);
            }
            LinkedNodes.AddIfDoesntExist(node);
            if (node is IStageNodeModable modable)
            {
                modable.EnemyMod = this;
            }
        }
        public void RevalidateNodes()
        {
            for (int i = 0; i < LinkedNodes.Count; i++)
            {
                if (LinkedNodes[i] == null)
                {
                    LinkedNodes.RemoveAt(i);
                    i--;
                }
            }
        }
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            RecordUndo("Modify Node Value");
            if (EF_Utility.EF_Button(Helper_BuildFieldRect(rect, ref index), "Start Link"))
            {
                stage.LinkStart(this);
            }
            RecordUndo("Modify Node Value");
            EnemyHealth = EF_Utility.EF_Slider(Helper_BuildFieldRect(rect, ref index), "Health", EnemyHealth, 1f, LargeHealthPool ? 10000f : 1000f);
            RecordUndo("Modify Node Value");
            LargeHealthPool = EF_Utility.EF_BoolField(Helper_BuildFieldRect(rect, ref index), "Large HealthPool", LargeHealthPool);
            RecordUndo("Modify Node Value");
            RevengeAttackOverride = EF_Utility.EF_ObjectField(Helper_BuildFieldRect(rect, ref index), nameof(RevengeAttackOverride), RevengeAttackOverride);
            RecordUndo("Modify Node Value");
            Indicator = EF_Utility.EF_BoolField(Helper_BuildFieldRect(rect, ref index), "Has Indicator", Indicator);
            RecordUndo("Modify Node Value");
            AmmoOverride = EF_Utility.EF_BoolField(Helper_BuildFieldRect(rect, ref index), "Ammo Override", AmmoOverride);
            if (AmmoOverride)
            {
                RecordUndo("Modify Node Value");
                AmmoSecondsOrCount = EF_Utility.EF_BoolField(Helper_BuildFieldRect(rect, ref index), "Ammo Seconds Or Count", AmmoSecondsOrCount);
                if (AmmoSecondsOrCount)
                {
                    RecordUndo("Modify Node Value");
                    ammoTime = EF_Utility.EF_Slider(Helper_BuildFieldRect(rect, ref index), "Ammo Seconds", ammoTime, 0.05f, 15f);
                }
                else
                {
                    RecordUndo("Modify Node Value");
                    ammoCount = EF_Utility.EF_Slider(Helper_BuildFieldRect(rect, ref index), "Ammo Count", ammoCount, 1, 100);
                }
            }
            RecordUndo("Modify Node Value");
            attackDelay = EF_Utility.EF_Slider(Helper_BuildFieldRect(rect, ref index), "Attack Delay", attackDelay, 0f, 5f);
            RecordUndo("Modify Node Value");
            if (actionOverride == null)
            {
                actionOverride = EF_Utility.EF_TypeDropdown<NodeActions.BaseAction>(Helper_BuildFieldRect(rect, ref index), "Action Override", actionOverride);
            }
            else
            {
                EditorGUI.LabelField(Helper_BuildFieldRect(rect, ref index), "Current Action", actionOverride.GetType().Name);
                if (EF_Utility.EF_Button(Helper_BuildFieldRect(rect, ref index), "Remove Action Override"))
                {
                    actionOverride = null;
                }
                DrawActionFields(actionOverride, Helper_BuildFieldRect(rect, ref index));
                EditorUtility.SetDirty(this);
            }
#endif
        }
        protected override Vector2 BuildSize()
        {
            float added = 0f;
            if (actionOverride != null)
            {
#if UNITY_EDITOR
                added += 20f * (CountDrawFields(actionOverride) + 1);
#endif
            }
            return new(450f, 400f + added);
        }
    }
}