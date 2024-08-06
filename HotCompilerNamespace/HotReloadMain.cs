using System.Reflection;
using EntityStates;
using EntityStates.GolemMonster;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using UnityEngine;

namespace HotCompilerNamespace
{
    public class HotReloadMain
    {
        const BindingFlags allFlags = (BindingFlags)(-1);

        public static void HotReloadEntryPoint()
        {
            // This is just for being able to call self.OnEnter() inside hooks.
            {
                new ILHook(typeof(HotReloadMain).GetMethod(nameof(BaseStateOnEnterCaller), allFlags), BaseStateOnEnterCallerMethodModifier);
            }

            {
                var methodToReload = typeof(FireLaser).GetMethod(nameof(FireLaser.OnEnter), allFlags);
                var newMethod = typeof(HotReloadMain).GetMethod(nameof(ModifiedGolemFireLaserOnEnter), allFlags);
                new Hook(methodToReload, newMethod);
            }
        }

        // This is just for being able to call self.OnEnter() inside hooks.
        private static void BaseStateOnEnterCaller(BaseState self)
        {

        }

        // This is just for being able to call self.OnEnter() inside hooks.
        private static void BaseStateOnEnterCallerMethodModifier(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(BaseState).GetMethod(nameof(BaseState.OnEnter), allFlags));
        }

        private static void ModifiedGolemFireLaserOnEnter(FireLaser self)
        {
            BaseStateOnEnterCaller(self);
            self.duration = FireLaser.baseDuration / self.attackSpeedStat;
            self.modifiedAimRay = self.GetAimRay();
            self.modifiedAimRay.direction = self.laserDirection;
            self.GetModelAnimator();
            Transform modelTransform = self.GetModelTransform();
            Util.PlaySound(FireLaser.attackSoundString, self.gameObject);
            string text = "MuzzleLaser";
            if (self.characterBody)
            {
                self.characterBody.SetAimTimer(2f);
            }
            self.PlayAnimation("Gesture", "FireLaser", "FireLaser.playbackRate", self.duration);
            if (FireLaser.effectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(FireLaser.effectPrefab, self.gameObject, text, false);
            }
            if (self.isAuthority)
            {
                float num = 1000f;
                Vector3 vector = self.modifiedAimRay.origin + self.modifiedAimRay.direction * num;
                RaycastHit raycastHit;
                if (Physics.Raycast(self.modifiedAimRay, out raycastHit, num, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.entityPrecise.mask))
                {
                    vector = raycastHit.point;
                }
                new BlastAttack
                {
                    attacker = self.gameObject,
                    inflictor = self.gameObject,
                    teamIndex = TeamComponent.GetObjectTeam(self.gameObject),
                    baseDamage = self.damageStat * FireLaser.damageCoefficient,
                    baseForce = FireLaser.force * 0.2f,
                    position = vector,
                    radius = FireLaser.blastRadius,
                    falloffModel = BlastAttack.FalloffModel.SweetSpot,
                    bonusForce = FireLaser.force * self.modifiedAimRay.direction
                }.Fire();
                Vector3 origin = self.modifiedAimRay.origin;
                if (modelTransform)
                {
                    ChildLocator component = modelTransform.GetComponent<ChildLocator>();
                    if (component)
                    {
                        int childIndex = component.FindChildIndex(text);
                        if (FireLaser.tracerEffectPrefab)
                        {
                            EffectData effectData = new EffectData
                            {
                                origin = vector,
                                start = self.modifiedAimRay.origin
                            };
                            effectData.SetChildLocatorTransformReference(self.gameObject, childIndex);
                            EffectManager.SpawnEffect(FireLaser.tracerEffectPrefab, effectData, true);
                            EffectManager.SpawnEffect(FireLaser.hitEffectPrefab, effectData, true);
                        }
                    }
                }
            }
        }
    }
}
