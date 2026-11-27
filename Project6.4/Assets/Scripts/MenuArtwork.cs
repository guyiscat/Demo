using UnityEngine;
namespace Seaside
{
    public sealed class MenuArtwork : ScriptableObject
    {
        static MenuArtwork current;
        public static MenuArtwork Current
        {
            get
            {
                if(current==null)current=Resources.Load<MenuArtwork>("MenuArtwork");
                if(current!=null)
                {
                    if(current.controls==null)current.controls=Resources.Load<Texture2D>("MenuUI/ControlsBackground");
                    if(current.display==null)current.display=Resources.Load<Texture2D>("MenuUI/DisplayBackground");
                    if(current.saveBackdrop==null)current.saveBackdrop=Resources.Load<Texture2D>("MenuUI/SaveBackground");
                }
                return current;
            }
        }
        public Texture2D controls,display,saveBackdrop;
    }
}
