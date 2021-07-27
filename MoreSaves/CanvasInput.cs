using Modding;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable CommentTypo

namespace MoreSaves
{
    // this is a modified version of what is used in Multiplayer Mod by jngo
    public class CanvasInput
    {
        private readonly GameObject InputObject;
        private readonly GameObject _textObject;
        private readonly GameObject _placeholderObj;
        private static InputField _inputField;
        
         public CanvasInput(GameObject parent, string name, Texture2D texture, Vector2 position, Vector2 size, Rect bgSubSection, Font font = null, string inputText = "", string placeholderText = "", int fontSize = 13)
        {
            if (size.x == 0 || size.y == 0)
            {
                size = new Vector2(bgSubSection.width, bgSubSection.height);
            }

            InputObject = new GameObject("Canvas Input - " + name);
            InputObject.AddComponent<CanvasRenderer>();
            RectTransform inputTransform = InputObject.AddComponent<RectTransform>();
            inputTransform.sizeDelta = new Vector2(bgSubSection.width, bgSubSection.height);
            Image image = InputObject.AddComponent<Image>();
            image.sprite = Sprite.Create(
                texture, 
                new Rect(0, 0, texture.width, texture.height),
                new Vector2( (float)texture.width / 2, (float) texture.height / 2)
            );
            image.type = Image.Type.Simple;
            _inputField = InputObject.AddComponent<InputField>();

            InputObject.transform.SetParent(parent.transform, false);
            inputTransform.SetScaleX(size.x / bgSubSection.width);
            inputTransform.SetScaleY(size.y / bgSubSection.height);
            
            Vector2 pos = new Vector2((position.x + ((size.x / bgSubSection.width) * bgSubSection.width) / 2f) / Screen.width, (Screen.height - (position.y + ((size.y / bgSubSection.height) * bgSubSection.height) / 2f)) / Screen.height);
            inputTransform.anchorMin = pos;
            inputTransform.anchorMax = pos;
            Object.DontDestroyOnLoad(InputObject);
            
            _placeholderObj = new GameObject("Canvas Input Placeholder - " + name);
            _placeholderObj.AddComponent<RectTransform>().sizeDelta = new Vector2(bgSubSection.width, bgSubSection.height);
            Text placeholderTxt = _placeholderObj.AddComponent<Text>();
            placeholderTxt.text = placeholderText;
            placeholderTxt.font = font;
            placeholderTxt.color = new Color(0, 0, 0, 0.5f);
            placeholderTxt.fontSize = fontSize;
            placeholderTxt.alignment = TextAnchor.MiddleCenter;
            _placeholderObj.transform.SetParent(InputObject.transform, false);
            Object.DontDestroyOnLoad(_placeholderObj);
            
            _textObject = new GameObject("Canvas Input Text - " + name);
            _textObject.AddComponent<RectTransform>().sizeDelta = new Vector2(bgSubSection.width, bgSubSection.height);
            Text textTxt = _textObject.AddComponent<Text>();
            textTxt.text = inputText;
            textTxt.font = CanvasUtil.TrajanBold;
            textTxt.fontSize = fontSize;
            textTxt.color = Color.black;
            textTxt.alignment = TextAnchor.MiddleCenter;
            _textObject.transform.SetParent(InputObject.transform, false);
            Object.DontDestroyOnLoad(_textObject);

            _inputField.targetGraphic = image;
            _inputField.placeholder = placeholderTxt;
            _inputField.textComponent = textTxt;
            _inputField.text = inputText;
        }
        
        public string GetText()
        {
            return InputObject != null ? _textObject.GetComponent<Text>().text : null;
        }

        public void ClearText()
        {
            if (InputObject == null) return;
            
            _inputField.text = "";
        }

        public void ChangePlaceholder(string text)
        {
            _placeholderObj.GetComponent<Text>().text = text;
        }

        public void Destroy()
        {
            Object.Destroy(InputObject);
            Object.Destroy(_placeholderObj);
            Object.Destroy(_textObject);
        }

    }
}