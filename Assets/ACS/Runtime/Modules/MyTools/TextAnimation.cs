using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ACS.TextAnimation
{   
    public class TextAnimation
    {
        private TextMeshProUGUI _textField;
        private string _text;
        private float _duration = 0.0f;
        private float _delay = 0.0f;
        private int _fontSize = 12;
        private bool _individualDelay = false;
        private bool _isTimeIndependent = false;
        private string _presetMessage = "";
        private System.Action _onComplete;
        private System.Action _onStart;
        private Coroutine _currentCoroutine;

        public TextAnimation(TextMeshProUGUI textField, string text, float duration)
        {
            _textField = textField;
            _text = text;
            _duration = duration;
        }

        public TextAnimation SetUpdate(bool isTimeIndependent)
        {
            _isTimeIndependent = isTimeIndependent;
            return this;
        }

        public TextAnimation SetFontSize(int fontSize)
        {
            _fontSize = fontSize;
            return this;
        }

        public TextAnimation SetPresetMessage(string presetMessage)
        {
            _presetMessage = presetMessage;
            //Debug.Log("Preset Message: " + _presetMessage);
            return this;
        }
             public TextAnimation SetDelay(float delay)
        {
            _delay = delay;
            return this;
        }
        
        public TextAnimation SetIndividualDelay(bool value)
        {
            _individualDelay = value;
            return this;
        } 

        public TextAnimation OnComplete(System.Action onComplete)
        {
            _onComplete = onComplete;
            return this;
        } 
        
        public TextAnimation OnStart(System.Action onStart)
        {
            _onStart = onStart;
            return this;
        }

        public void WriteLetter()
        {
            if (_currentCoroutine == null)
            {
                _currentCoroutine = _textField.StartCoroutine(WriteLetterCoroutine());
                TextExtensions._activeAnimations[_textField] = this;
            }
        }

        public void WriteWord()
        {
            if (_currentCoroutine == null)
            {
                _currentCoroutine = _textField.StartCoroutine(WriteWordCoroutine());
                TextExtensions._activeAnimations[_textField] = this;
            }
        }

        public void Start(bool isLetter = true)
        {
            if (_currentCoroutine == null)
            {
                if (isLetter)
                    _currentCoroutine = _textField.StartCoroutine(WriteLetterCoroutine());
                else
                    _currentCoroutine = _textField.StartCoroutine(WriteWordCoroutine());
                TextExtensions._activeAnimations[_textField] = this;
            }
        }

        public void Kill()
        {
            if (_currentCoroutine != null)
            {
                _textField.StopCoroutine(_currentCoroutine);
                _currentCoroutine = null;
                _textField.text = ""; // Reset the text to the preset message or clear it
            }

            if (TextExtensions._activeAnimations.ContainsKey(_textField))
            {
                TextExtensions._activeAnimations.Remove(_textField);
            }
        }

        private IEnumerator WriteLetterCoroutine()
        {
            _textField.fontSize = _fontSize;
            _textField.text = _presetMessage;
            float delay =_individualDelay ? _duration : _duration / _text.Length;
            if (_isTimeIndependent)
                yield return new WaitForSecondsRealtime(_delay);
            else
                yield return new WaitForSeconds(_delay);
            _onStart?.Invoke();
            for (int i = 0; i < _text.Length; i++)
            {
                _textField.text += _text[i];
                if (_isTimeIndependent)
                    yield return new WaitForSecondsRealtime(delay);
                else
                    yield return new WaitForSeconds(delay);
            }
            if (TextExtensions._activeAnimations.ContainsKey(_textField))
            {
                TextExtensions._activeAnimations.Remove(_textField);
            }
            _onComplete?.Invoke();
            _currentCoroutine = null;
        }

        private IEnumerator WriteWordCoroutine()
        {
            _textField.fontSize = _fontSize;
            _textField.text = _presetMessage;
            string[] words = _text.Split(' ');
            float delay =_individualDelay ? _duration : _duration / words.Length;
            if (_isTimeIndependent)
                yield return new WaitForSecondsRealtime(_delay);
            else
                yield return new WaitForSeconds(_delay);
            _onStart?.Invoke();
            foreach (string word in words)
            {
                if (_textField.text.Length > 0)
                {
                    _textField.text += " ";
                }
                _textField.text += word;
                if (_isTimeIndependent)
                    yield return new WaitForSecondsRealtime(delay);
                else
                    yield return new WaitForSeconds(delay);
            }
            if (TextExtensions._activeAnimations.ContainsKey(_textField))
            {
                TextExtensions._activeAnimations.Remove(_textField);
            }
            _onComplete?.Invoke();
            _currentCoroutine = null;
        }
    }

    public static class TextExtensions
    {
        public static Dictionary<TextMeshProUGUI, TextAnimation> _activeAnimations = new();

        public static TextAnimation WriteLetter(this TextMeshProUGUI textField, string text, float duration)
        {
            return new TextAnimation(textField, text, duration);
        }

        public static TextAnimation WriteWord(this TextMeshProUGUI textField, string text, float duration)
        {
            return new TextAnimation(textField, text, duration);
        }

        public static void Kill(this TextMeshProUGUI textField)
        {
            if (_activeAnimations.TryGetValue(textField, out TextAnimation animation))
            {
                animation.Kill();
            }
        }

    }


}




