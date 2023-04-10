using UnityEngine;

namespace Assets {
    public static class GameObjectExtension {
        public static GameObject WithChild(this GameObject parent, string name) {
            foreach (Transform child in parent.transform) {
                if (child.name == name) {
                    return child.gameObject;
                }
            }
            return null;
        }
    }
}
