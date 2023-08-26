using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using FivePD.API;
using FivePD.API.Utils;
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
        Ped tsDriver = null;
        Vehicle tsVehicle = null;
        Ped player = null;
        int counter = 0;

        // Now we are starting a ticker. This ticket will check every X amount of time if the player is performing a traffic stop or not.
        internal TrafficStopPlugin()
        {
            // This will triger the enhanced traffic stops
            Tick += CheckForTrafficStop;
            // This will clear the enhanced traffic stop status if player is no longer on a traffic stop.
            Tick += clearEts;
        }


        // In this function we will check if the user is currently on a traffic stop
        public async Task CheckForTrafficStop()
        {
            counter++;
            Screen.ShowNotification("CHECKING NEW ETS #" + counter);
            Debug.WriteLine("CHECKING NEW ETS #" + counter);
            // This is to avoid ticks getting triggered 1000 times a second.
            await (BaseScript.Delay(5000));
            //Check to see if player is on a traffic stop

            // TEST TO TRY TO FIX THE STUPID BUG
            // Basically everytime this script runs, if we are currently not on a traffic stop we'll reset the driver to be null.

            /// 
            
            if (Utilities.IsPlayerPerformingTrafficStop())
            {
                //Wait till player steps out of the vehicle to show they are about to do the interaction
                //if (!Game.PlayerPed.IsInVehicle())
                //{
                // In this section we will determien if the player is currently not on an enhanced traffic stop and proceed to make one
                // NOTE: In the future, this will have a randomizer to determine at random if this traffic stop will be enhanced or not.
                if (!enhancedTrafficStop)
                {
                    // First we will set the OfferedCallout to TRUE to avoid repeating the function. This will cancel automatically once the ped has been arrested or killed.
                    enhancedTrafficStop = true;

                    // Now we're going to define who is the driver, and who is the player.
                        
                    tsDriver = Utilities.GetDriverFromTrafficStop();

                    tsVehicle = Utilities.GetVehicleFromTrafficStop();
                    player = Game.PlayerPed;

                    // First we will check that the traffic stop did not turn into a pursuit right away so we can keep those happening as usual
                    if (tsDriver.IsFleeing)
                    {
                        // If it is a vehicle pursuit, we will trigger a return to stop the function from executing.
                        return;
                    }

                    Screen.ShowNotification("Initiated enhanced traffic stop on PED ID: " + tsDriver.NetworkId);
                    Debug.WriteLine("Initiated enhanced traffic stop on PED ID: " + tsDriver.NetworkId);
                    await triggerScenario(PERSONALITY.EVIL.SHOOT_WHEN_CLOSE, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.ATTACK_WITH_MELEE, tsDriver, player, tsVehicle);

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
            if (randomPersonality <= 50) // If result is under 60, it will be lawful
            {               
                if (randomReaction >= 1 && randomReaction <= 50) return PERSONALITY.LAWFUL.STAY;
                else if (randomReaction >= 51 && randomReaction <= 75) return PERSONALITY.LAWFUL.EXIT_VEHICLE;
                else if (randomReaction >= 76 && randomReaction <= 100) return PERSONALITY.LAWFUL.WALK_TOWARDS_OFFICER;
            }   
            else if (randomPersonality > 50 && randomPersonality <= 80) // If the result is between 60 or 90 it will be coward
            {
                if (randomReaction >= 1 && randomReaction <= 20) return PERSONALITY.COWARD.FLEE_ON_FOOT;
                else if (randomReaction >= 21 && randomReaction <= 40) return PERSONALITY.COWARD.HIDE_BEHIND_VEHICLE;
                else if (randomReaction >= 41 && randomReaction <= 70) return PERSONALITY.COWARD.VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE;
                else if (randomReaction >= 71 && randomReaction <= 100) return PERSONALITY.COWARD.VEHICLE_FLEE_AT_RANDOM;
            }
            else if (randomPersonality > 80 && randomPersonality <= 100) // And if the result is between 90 and 100 it will be evil
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

        private async Task triggerScenario(int PERSONALITY_TYPE, Ped targetPed, Ped player, Vehicle targetVehicle)
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
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

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
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Lastly, we fight!
                targetPed.Task.FightAgainst(player);
                return;
            }

            if (PERSONALITY_TYPE == PERSONALITY.EVIL.SHOOT_WHEN_CLOSE)
            {
                bool exit = false;
                while (!exit)
                {
                    // For optimization purposes
                    await BaseScript.Delay(1000);
                    // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                    // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                    if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                    // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                    if(World.GetDistance(player.Position, targetPed.Position) <= 3f) { exit = true; }
                    
                }

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
                if(isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                // Lastly, the ped flees away
                targetPed.Task.FleeFrom(player);


                /////////////

                // Here we will upgrade this scenario with an extra level or randomness.
                // With a 50/50 chance, the ped will flee forever until caught, or will get out of the vehicle to fight, or flee on foot.
                int randomUpgrade = RandomUtils.GetRandomNumber(1, 101);
                // TEST
                randomUpgrade = 53;

                if(randomUpgrade > 50)
                {
                    // Now we will check if we will make him fight or escape on foot
                    if (randomUpgrade >= 51 && randomUpgrade <= 75)
                    {
                        // First we wait some time from the ped fleeing from the officer
                        await (Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_30)));
                        // Now we make the ped get out of the vehicle
                        targetPed.Task.LeaveVehicle();
                        await (BaseScript.Delay(2500));
                        if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                        targetPed.Task.ClearAll();
                        targetPed.Task.HandsUp(30000);
                        
                        // Extra scenario with 5% chance for the ped to try to run on foot after being stopped, or shooting back at the player.
                        if(randomUpgrade >=51 && randomUpgrade <= 55)
                        {
                            await (Delay(RandomUtils.GetRandomNumber(10000, 15000)));
                            if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                            if (!targetPed.IsCuffed)
                            {
                                targetPed.Task.ReactAndFlee(player);
                            }
                        } 
                        else if (randomUpgrade >= 71 && randomUpgrade <= 75)
                        {
                            await (Delay(RandomUtils.GetRandomNumber(5000, 10000)));
                            if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                            targetPed.Task.ShootAt(player);
                        }
                    }
                    
                }               
               
                return;
            }
        }

        private bool isPedEmpty(Ped targetPed)
        {
            if (targetPed == null || targetPed.NetworkId == 0)
                return true; 
            else 
                return false;
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

        
        private async Task clearEts()
        {
            // This is to avoid ticks getting triggered 1000 times a second.
            await (BaseScript.Delay(3000));

            // First we verify if the player is currently in a ETS
            if (enhancedTrafficStop)
            {
                // Now we verify if the player is no longer on a traffic stop.
                if (!Utilities.IsPlayerPerformingTrafficStop()) 
                {
                    // If these conditions are true, it means that the player just got off an enhanced traffic stop, and we should reset his status so he can take another one.
                    enhancedTrafficStop = false;
                   
                    // Additionally, we are going to add a new task to the ped so it can go back to normal instead of just staying in the middle of the road
                    if(!isPedEmpty(tsDriver))
                    {
                        // First we clear all it's previous tasks
                        tsDriver.Task.ClearAll();

                        // Now we determine if they are still in a car or not
                        if(tsVehicle != null && tsVehicle.NetworkId != 0)
                        {
                            // If they are in a car, we'll set them to roam arond in the car
                            tsDriver.Task.CruiseWithVehicle(tsVehicle, 30f, (int)DrivingStyle.Rushed);
                        }
                        else
                        {   // If they are on foot, we'll se them to walk away.
                            tsDriver.Task.WanderAround();
                        }
                            
                    }                 

                }
            }
        }


    }
}
