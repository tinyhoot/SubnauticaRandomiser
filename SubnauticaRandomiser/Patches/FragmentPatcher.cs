using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace SubnauticaRandomiser.Patches
{
    // [HarmonyPatch]
    public class FragmentPatcher
    {
        /// <summary>
        /// This method patches a few lines into PDAScanner.Scan() to intercept the game's normal operations.
        /// Instead of hard-coding two titanium on scanning a duplicate fragment, the game will instead call
        /// YieldMaterial() in this class here.
        /// </summary>
        /// <param name="codeInstructions">The IL code of the function.</param>
        /// <returns>The transpiled, modified code.</returns>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PDAScanner), nameof(PDAScanner.Scan))]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            FileLog.Log("[F] Starting transpiler for duplicate scan results.");

            List<CodeInstruction> instructions = new List<CodeInstruction>(codeInstructions);

            string errorMsgBeforeMethodCall = "ScannerRedundantScanned";
            int methodArgsIndex = 0;

            // Before the crucial call that adds two titanium on duplicate scan,
            // the game actually logs an error message. We abuse this fact to
            // easily find the code lines we need.
            for (int i = 0; i < instructions.Count; i++)
            {
                if (!instructions[i].Is(OpCodes.Ldstr, errorMsgBeforeMethodCall))
                    continue;
                
                // Found the debug error message just before the method we need to alter.
                // The part we're here for is somewhere in the next few lines.
                for (int j = 1; j < 5; j++)
                {
                    // The crucial line is a four-argument call to CraftData.AddToInventory().
                    // The first of those arguments is TechType Titanium.
                    if (instructions[i+j].Is(OpCodes.Ldc_I4_S, 16))
                    {
                        // Found the instruction pushing TechType 16 (Titanium) onto the stack.
                        methodArgsIndex = i + j;
                        FileLog.Log("[F] Found arg0 Titanium at index " + methodArgsIndex);

                        // The original method takes four arguments, but the replacement needs
                        // only one. Replace the first argument with the scan target (conveniently
                        // stored in local variable 0) and the others with NOP to preserve continuity.
                        instructions[methodArgsIndex].opcode = OpCodes.Ldloc_0;
                        instructions[methodArgsIndex + 1].opcode = OpCodes.Nop;
                        instructions[methodArgsIndex + 2].opcode = OpCodes.Nop;
                        instructions[methodArgsIndex + 3].opcode = OpCodes.Nop;
                        instructions[methodArgsIndex + 4].operand
                            = typeof(FragmentPatcher).GetMethod("YieldMaterial", new[] { typeof(TechType) });

                        FileLog.Log("[F] Successfully altered CodeInstructions.");
                        break;
                    }
                }
                break;
            }

            if (methodArgsIndex == 0)
                FileLog.Log("[F] Failed to find argument index while trying to transpile fragment scan rewards!");

            return instructions.AsEnumerable();
        }

        /// <summary>
        /// Add a random material to the player's inventory upon scanning an already known fragment.
        /// </summary>
        /// <param name="target">The fragment being scanned.</param>
        public static void YieldMaterial(TechType target)
        {
            // If the options for yields were not randomised, just go with the game's default behaviour.
            if (!(InitMod.s_masterDict?.FragmentMaterialYield?.Count > 0))
            {
                CraftData.AddToInventory(TechType.Titanium, 2, false, true);
                return;
            }

            Random rand = new Random();
            TechType type = GetRandomMaterial(rand);
            int number = rand.Next(1, InitMod.s_config?.iMaxDuplicateScanYield + 1 ?? 4);
            FileLog.Log($"[F] Replacing duplicate fragment scan yield of target {target.AsString()} with "
                             + type.AsString());
            CraftData.AddToInventory(type, number, false, true);
        }

        /// <summary>
        /// Choose a random weighted material for duplicate scan rewards.
        /// </summary>
        /// <param name="rand">An instance of Random.</param>
        /// <returns>The TechType of the chosen material, or Titanium if an error occurred.</returns>
        private static TechType GetRandomMaterial(Random rand)
        {
            if (!(InitMod.s_masterDict?.FragmentMaterialYield?.Count > 0))
                return TechType.Titanium;

            double sumOfWeights = InitMod.s_masterDict.FragmentMaterialYield.Sum(x => x.Value);
            double choice = sumOfWeights * rand.NextDouble();

            // Add up the weights of the material options until the value of 'choice' is exceeded, and choose that one.
            double sum = 0.0;
            foreach (var kv in InitMod.s_masterDict.FragmentMaterialYield)
            {
                sum += kv.Value;
                if (sum >= choice)
                    return kv.Key;
            }

            FileLog.Log("[F] Failed to choose random material for duplicate fragment scan.");
            return TechType.Titanium;
        }
    }
}