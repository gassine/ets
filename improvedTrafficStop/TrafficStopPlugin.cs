using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using FivePD.API;
using FivePD.API.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrafficStopPlugin
{
    internal class TrafficStopPlugin : Plugin
    {

        // Here we will create a class to track the personality types
        public class PERSONALITY
        {
            public class LAWFUL
            {
                public static int STAY = 100;
                public static int EXIT_VEHICLE = 101;
                public static int WALK_TOWARDS_OFFICER = 102;
            }

            public class EVIL
            {
                public static int FIST_FIGHT = 200;
                public static int SHOOT_WHEN_CLOSE = 201;
                public static int SHOOT_WHEN_OFFICER_IS_OUT = 202;
                public static int SHOOT_AT_RANDOM_TIME = 203;
                public static int ATTACK_WITH_MELEE = 204;
                public static int SHOOT_FROM_START = 205;
            }

            public class COWARD
            {
                public static int FLEE_ON_FOOT = 300;
                public static int HIDE_BEHIND_VEHICLE = 301;
                public static int VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE = 302;
                public static int VEHICLE_FLEE_AT_RANDOM = 303;
            }

        }

        public class TIME
        {
            public static int SECONDS_1 = 1000;
            public static int SECONDS_5 = 5 * 1000;
            public static int SECONDS_15 = 15 * 1000;
            public static int SECONDS_30 = 30 * 1000;
            public static int SECONDS_45 = 45 * 1000;
            public static int MINUTES_1 = 1 * 60 * 1000;
            public static int MINUTES_5 = 5 * 60 * 1000;
            public static int MINUTES_10 = 10 * 60 * 1000;
        }
        // END OF PERSONALITY TYPES 


        // Here we are setting that variable where we will track if the user is on a callout or not.
        private bool enhancedTrafficStop = false;

        // Now we are starting a ticker. This ticket will check every X amount of time if the player is performing a traffic stop or not.
        internal TrafficStopPlugin()
        {
            // This will triger the enhanced traffic stops
            Tick += CheckForTrafficStop;    
            // This will clear the enhanced traffic stop status if player is no longer on TS.
            Tick += clearEts;
        }


        // In this function we will check if the user is currently on a traffic stop
        public async Task CheckForTrafficStop()
        {
            //Check to see if player is on a traffic stop
            if (Utilities.IsPlayerPerformingTrafficStop())
            {
                //Wait till player steps out of the vehicle to show they are about to do the interaction
                //if (!Game.PlayerPed.IsInVehicle())
                // First we will check that the traffic stop did not turn into a pursuit right away so we can keep those happening as usual
                if (Utilities.GetDriverFromTrafficStop().IsFleeing) 
                {
                    // If it is a vehicle pursuit, we will trigger our variable to stop the enhancement from happening until this traffic stop is over.
                    enhancedTrafficStop = true;
                }
                //{
                    // In this section we will determien if the player is currently not on an enhanced traffic stop and proceed to make one
                    // NOTE: In the future, this will have a randomizer to determine at random if this traffic stop will be enhanced or not.
                    if (enhancedTrafficStop == false || enhancedTrafficStop == null)
                    {
                        // First we will set the OfferedCallout to TRUE to avoid repeating the function. This will cancel automatically once the ped has been arrested or killed.
                        enhancedTrafficStop = true;

                        // Now we're going to define who is the driver, and who is the player.
                        
                        Ped tsDriver = Utilities.GetDriverFromTrafficStop();
                        Ped player = Game.PlayerPed;

                        Screen.ShowNotification("Initiated enhanced traffic stop on PED ID: " + tsDriver.NetworkId);
                        await triggerScenario(PERSONALITY.EVIL.SHOOT_WHEN_CLOSE, tsDriver, player);
                        //await triggerScenario(PERSONALITY.EVIL.ATTACK_WITH_MELEE, tsDriver, player);

                        // Now we will assign a personality at random to the ped.

                        /*
                        // Here we are getting the data from the suspect
                        suspectData = await Utilities.GetPedData(tsDriver.NetworkId);
                        //suspectData.Violations = new List<Violation>();                                             
                        Screen.ShowNotification("Current offence: " + newViolation.Offence + " Charge: " + newViolation.Charge);
                        await (Delay(2000));                     
                        // Now we set the new data
                        Utilities.SetPedData(tsDriver.NetworkId, suspectData);
                        */

                        /*
                        // Now we are going to make him go ape shit and do stupid shit.
                        tsDriver.Weapons.Give(WeaponHash.MiniSMG, 9999, true, true);
                        // And now we will make the driver shoot the player
                        tsDriver.Task.LeaveVehicle();

                        // Allowing driver the time to get out of the vehicle so the instruction can execute.

                        await BaseScript.Delay(1500);
                        tsDriver.Task.AimAt(player, 10000);

                        await BaseScript.Delay(5000);

                        tsDriver.Task.HandsUp(5000);
                        //tsDriver.Task.ShootAt(player);
                        */
                    }
                //}

                // Here we might be able to add a section to check for the bug of peds shooting even after being arrested
                // Something along the lines of checking if the ped is arrested, then have him surrender/stop attacking/shooting
            }

            // I don't know what this is for, but seems to be necessary.
            await Task.FromResult(0);
        }

        private int getRandomPersonality()
        {
            // First we get a random number
            int randomPersonality = RandomUtils.GetRandomNumber(1, 101);
            int randomReaction = RandomUtils.GetRandomNumber(1, 101);

            // Now we return a type of personality based on the number.
            // First we will determine if the personality is lawful, evil or coward
            if (randomPersonality <= 60) // If result is under 60, it will be lawful
            {               
                if (randomReaction >= 1 && randomReaction <= 50) return PERSONALITY.LAWFUL.STAY;
                else if (randomReaction >= 51 && randomReaction <= 75) return PERSONALITY.LAWFUL.EXIT_VEHICLE;
                else if (randomReaction >= 76 && randomReaction <= 100) return PERSONALITY.LAWFUL.WALK_TOWARDS_OFFICER;
            }   
            else if (randomPersonality > 60 && randomPersonality <= 90) // If the result is between 60 or 90 it will be coward
            {
                if (randomReaction >= 1 && randomReaction <= 20) return PERSONALITY.COWARD.FLEE_ON_FOOT;
                else if (randomReaction >= 21 && randomReaction <= 40) return PERSONALITY.COWARD.HIDE_BEHIND_VEHICLE;
                else if (randomReaction >= 41 && randomReaction <= 70) return PERSONALITY.COWARD.VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE;
                else if (randomReaction >= 71 && randomReaction <= 100) return PERSONALITY.COWARD.VEHICLE_FLEE_AT_RANDOM;
            }
            else if (randomPersonality > 90 && randomPersonality <= 100) // And if the result is between 90 and 100 it will be evil
            {
                if (randomReaction >= 1 && randomReaction <= 30) return PERSONALITY.EVIL.SHOOT_WHEN_CLOSE;
                else if (randomReaction >= 31 && randomReaction <= 40) return PERSONALITY.EVIL.FIST_FIGHT;
                else if (randomReaction >= 41 && randomReaction <= 60) return PERSONALITY.EVIL.SHOOT_AT_RANDOM_TIME;
                else if (randomReaction >= 61 && randomReaction <= 70) return PERSONALITY.EVIL.SHOOT_FROM_START;
                else if (randomReaction >= 81 && randomReaction <= 90) return PERSONALITY.EVIL.ATTACK_WITH_MELEE;
                else if (randomReaction >= 91 && randomReaction <= 100) return PERSONALITY.EVIL.SHOOT_WHEN_OFFICER_IS_OUT;
            }

            // If for some reason it didn't fall under any other category, then we will let it continue as usual.
            return PERSONALITY.LAWFUL.STAY;
        }

        private async Task triggerScenario(int PERSONALITY_TYPE, Ped targetPed, Ped player)
        {
            // Now we will check which personality profile do we have.

            /* -------------------------------------------------
               -------------------------------------------------
               --------------------EVIL BLOCK-------------------           
               -------------------------------------------------
               ------------------------------------------------- */

            if (PERSONALITY_TYPE == PERSONALITY.EVIL.FIST_FIGHT) {
                // In this scenario, the ped will get out of the vehicle as soon as the officer gets out and will charge at him with his bare fist.

                // Here we are setting a random timer for the action to begin.
                //await( Delay(RandomUtils.GetRandomNumber(1000, 30000)));
                targetPed.Task.LeaveVehicle();

                // Now we allowing time for the driver to get out of the vehicle so the instruction can execute.
                await BaseScript.Delay(1500);

                // Lastly, we fight!
                targetPed.Task.FightAgainst(player);
                return;
            }

            if(PERSONALITY_TYPE == PERSONALITY.EVIL.ATTACK_WITH_MELEE)
            {
                // Here we are setting a random timer for the action to begin.
                await( Delay(RandomUtils.GetRandomNumber(1000, 30000)));

                // First we equip a random melee weapon
                targetPed.Weapons.Give(GetMeleeWeapon(), 1, true, true);

                // Now we ask the ped to leave the vehicle
                targetPed.Task.LeaveVehicle();

                // Now we allowing time for the driver to get out of the vehicle so the instruction can execute.
                await BaseScript.Delay(1500);

                // Lastly, we fight!
                targetPed.Task.FightAgainst(player);
                return;
            }

            if (PERSONALITY_TYPE == PERSONALITY.EVIL.SHOOT_WHEN_CLOSE)
            {
                // Here we are setting a random timer for the action to begin.
                while (World.GetDistance(player.Position, targetPed.Position) > 3f) { await BaseScript.Delay(100); }

                // TEST To see if he will still keep shooting while running away
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // First we equip a random handgun
                targetPed.Weapons.Give(getHandgun(), 200, true, true);

                // Now we will roll down the window to avoid the PED breaking the window to shoot.
                Vehicle tsVehicle = Utilities.GetVehicleFromTrafficStop();
                API.RollDownWindow(tsVehicle.Handle, 0);

                // Now we set the shooting rate to be fast
                targetPed.ShootRate = 1000;
                // And lastly, we trigger the action to shoot at the player
                targetPed.Task.VehicleShootAtPed(player);

                // Now we allowing time before the PED flees
                await BaseScript.Delay(6000);

                // Lastly, the ped flees away
                targetPed.Task.FleeFrom(player);


                // TO BE DEVELOPED: Add a randomizer here that will make a 50/50 chance for the ped to decide to continue the pursuit till the end
                // or stop after a random amount of time. If he stops, he will have a 33/33/33 chance to surrender with his hands up, flee on foot, or shoot at the person pursuing him
                // Testing: After a random amount of time in pursuit, the ped will get out and start shooting at you.
                int randomUpgrade = RandomUtils.GetRandomNumber(1, 101);

                // Here we will upgrade this scenario with an extra level or randomness.
                // With a 50/50 chance, the ped will flee forever until caught, or will get out of the vehicle to fight, or flee on foot.
                if(randomUpgrade > 50)
                {
                    // Now we will check if we will make him fight or escape on foot
                    if (randomUpgrade >= 51 && randomUpgrade <= 100)
                    {
                        // First we wait some time from the ped fleeing from the officer
                        await (Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_30)));
                        // Now we make the ped get out of the vehicle
                        targetPed.Task.LeaveVehicle();
                    }
                    
                    targetPed.Task.LeaveVehicle();

                    await BaseScript.Delay(1500);
                    targetPed.Task.ShootAt(player);
                }

               
                return;
            }
        }

        private WeaponHash GetMeleeWeapon()
        {
            List<WeaponHash> weapons = new List<WeaponHash>()
            {
                WeaponHash.Knife,
                WeaponHash.SwitchBlade,
                WeaponHash.Bat,
                WeaponHash.Wrench,
                WeaponHash.Bottle,
                WeaponHash.Crowbar,
                WeaponHash.GolfClub,
                WeaponHash.PoolCue,
                WeaponHash.Machete,
            };

            return weapons.SelectRandom();
        }

        private WeaponHash getHandgun()
        {
            List<WeaponHash> weapons = new List<WeaponHash>()
            {
                WeaponHash.CombatPistol,
                WeaponHash.HeavyPistol,
                WeaponHash.MicroSMG,
                WeaponHash.Pistol,
                WeaponHash.Pistol50,
                WeaponHash.Revolver,
            };

            return weapons.SelectRandom();
        }

        /*
        private async Task clearEts()
        {
            // First we verify if the player is currently in a ETS
            if (!Utilities.IsPlayerPerformingTrafficStop())
            {
                // Now we verify if the player is no longer on a traffic stop.
                //if (!enhancedTrafficStop)
                //{
                    // If these conditions are true, it means that the player just got off an enhanced traffic stop, and we should reset his status so he can take another one.
                    Screen.ShowNotification("Clearing ETS Variable");
                    enhancedTrafficStop = false;
                //}
            }
        }
        */

        
        private async Task clearEts()
        {
            // First we verify if the player is currently in a ETS
            if (enhancedTrafficStop == true || enhancedTrafficStop == null)
            {
                // Now we verify if the player is no longer on a traffic stop.
                if (!Utilities.IsPlayerPerformingTrafficStop()) 
                {
                // If these conditions are true, it means that the player just got off an enhanced traffic stop, and we should reset his status so he can take another one.
                Screen.ShowNotification("Clearing ETS Variable");
                enhancedTrafficStop = false;
                }
            }
        }
    }
}
