using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrickbreakerBase
{
    // One pip per max-HP point, each shown filled or empty depending on current HP. Pips are
    // created (or removed) to match GetMaxHp() on every refresh, so changing MaxHp mid-session
    // just works.
    public class HealthHUD : BaseHUD
    {
        public Transform PipContainer;
        public Vector2 PipSize = new Vector2(40f, 40f);
        public Color FilledColor = new Color(0.9f, 0.25f, 0.3f, 1f);
        public Color EmptyColor = new Color(0.25f, 0.25f, 0.3f, 0.6f);

        private readonly List<Image> _pips = new List<Image>();

        protected override void Start()
        {
            base.Start();

            GameManager.Instance.OnHpChanged.AddListener(RefreshPips);

            RefreshPips(GameManager.Instance.GetHp());
        }

        private void RefreshPips(int currentHp)
        {
            int maxHp = GameManager.Instance.GetMaxHp();

            while (_pips.Count < maxHp)
            {
                Image pip = UIFactory.CreateImage(PipContainer, "Pip", FilledColor);
                pip.rectTransform.sizeDelta = PipSize;
                _pips.Add(pip);
            }

            while (_pips.Count > maxHp)
            {
                Image last = _pips[_pips.Count - 1];
                _pips.RemoveAt(_pips.Count - 1);
                Destroy(last.gameObject);
            }

            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].color = i < currentHp ? FilledColor : EmptyColor;
            }
        }
    }
}
