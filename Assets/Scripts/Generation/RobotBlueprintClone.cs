using RoboterLego.Domain;

namespace RoboterLego.Generation
{
    public static class RobotBlueprintClone
    {
        public static RobotBlueprint Clone(RobotBlueprint blueprint)
        {
            if (blueprint == null)
            {
                return null;
            }

            var clone = new RobotBlueprint
            {
                CoreModuleId = blueprint.CoreModuleId,
                BehaviorProfile = new BehaviorProfile
                {
                    MoveStyle = blueprint.BehaviorProfile?.MoveStyle,
                    DanceStyle = blueprint.BehaviorProfile?.DanceStyle,
                    SingStyle = blueprint.BehaviorProfile?.SingStyle,
                    Energy = blueprint.BehaviorProfile != null ? blueprint.BehaviorProfile.Energy : 0.4f
                }
            };

            if (blueprint.LimbModuleIds != null)
            {
                clone.LimbModuleIds.AddRange(blueprint.LimbModuleIds);
            }

            if (blueprint.AccessoryModuleIds != null)
            {
                clone.AccessoryModuleIds.AddRange(blueprint.AccessoryModuleIds);
            }

            return clone;
        }
    }
}
