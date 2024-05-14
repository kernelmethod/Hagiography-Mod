using XRL;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;
using XRL.World;

namespace Kernelmethod.Hagiography {
    public class BuildCodeSetterModule : AbstractEmbarkBuilderModule {
        public override void bootGame(XRLGame game, EmbarkInfo info) {
            game.SetStringGameState(Constants.BUILD_CODE_STATE, builder.generateCode());
        }
    }
}
