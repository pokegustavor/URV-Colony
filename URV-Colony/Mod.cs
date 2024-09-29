using PulsarModLoader;

namespace URV_Colony
{
    public class Mod : PulsarMod
    {
        public override string Version => "1.2";

        public override string Author => "pokegustavo";

        public override string ShortDescription => "Add the URV-500 to the lost colony";

        public override string Name => "Lost Umbra";

        public override int MPRequirements => 3;

        public override string HarmonyIdentifier()
        {
            return "pokegustavo.UmbraColony";
        }
    }
}
