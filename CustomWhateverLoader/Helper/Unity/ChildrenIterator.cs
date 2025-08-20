using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cwl.Helper.Unity;

public static class ChildrenIterator
{
    extension(Transform parent)
    {
        public IEnumerable<Transform> GetAllChildren(bool active = false)
        {
            for (var i = 0; i < parent.childCount; ++i) {
                var child = parent.GetChild(i);
                if (active && !child.gameObject.activeInHierarchy) {
                    continue;
                }

                yield return child;
            }
        }

        public Transform? GetFirstChildWithName(string name)
        {
            var children = parent
                .GetAllChildren()
                .Where(t => t.name == name);
            var transforms = children.ToArray();
            return transforms.TryGet(0, true);
        }

        public Transform? GetFirstNestedChildWithName(string name)
        {
            var child = parent;
            foreach (var hierarchy in name.Split('/')) {
                child = child.GetFirstChildWithName(hierarchy);
                if (child is null) {
                    break;
                }
            }

            return child;
        }
    }
}