using ReadyPlayerMe.AvatarCreator;

namespace ReadyPlayerMe
{
    public class AvatarCreatorData
    {
        public AvatarProperties AvatarProperties;
        
        public void Awake()
        {
            AvatarProperties = new AvatarProperties();
        }
    }
}
