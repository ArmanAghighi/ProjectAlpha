namespace echo17.EndlessBook
{
    using System;
    using UnityEngine;

    [Serializable]
    public class PageData
    {
        public Material material;
        public bool hasUI;
        public UISO UI;
        static public PageData Default()
        {
            return new PageData()
            {
                material = null,
                hasUI = false,
                UI = null
            };
        }
    }
}