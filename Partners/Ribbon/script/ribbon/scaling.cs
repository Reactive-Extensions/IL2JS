using System;
using System.Collections.Generic;

namespace Ribbon
{
    internal class ScalingStep
    {
        string _groupId;
        string _layoutName;
        string _popupSize;

        // For LowScaleWarning
        string _scaleWarningMessage;
        bool _warnForScale;
        Scaling _scalingInfo;

        public ScalingStep(string groupId,
                           string layoutName,
                           string popupSize,
                           string scaleWarningMessage,
                           bool warnForScale)
        {
            if ((string.IsNullOrEmpty(groupId) || 
                string.IsNullOrEmpty(layoutName)))
                throw new ArgumentNullException("groupId, layoutName and message cannot be undefined or null");

            _groupId = groupId;
            _layoutName = layoutName;
            _popupSize = popupSize;
            _scaleWarningMessage = scaleWarningMessage;
            _warnForScale = warnForScale;
        }

        internal void SetParent(Scaling parent)
        {
            _scalingInfo = parent;
        }

        public string GroupId
        {
            get 
            { 
                return _groupId; 
            }
        }

        public string LayoutName
        {
            get 
            { 
                return _layoutName; 
            }
        }

        public string PopupSize
        {
            get 
            { 
                return _popupSize; 
            }
        }

        public string ScaleWarningMessage
        {
            get 
            { 
                return _scaleWarningMessage; 
            }
        }

        public bool HasScaleWarning
        {
            get 
            { 
                return _warnForScale; 
            }
        }

        public string PreviousLayoutName
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_scalingInfo))
                    return string.Empty;

                // We need to get the previous layout name that was used in the previous scale for this group
                // So, we set the name to the maximum size and then loop through the scaling steps and see
                // if we can find any other scales for this group.
                string prevName = _scalingInfo.GetGroupMaxSize(_groupId);
                List<ScalingStep> steps = _scalingInfo.StepsInternal;
                for (int i = 0; i < steps.Count; i++)
                {
                    ScalingStep step = steps[i];
                    // When we get to this step, we exit since we are looking for "previous layout names"
                    if (step == this)
                        break;

                    // When we find a previous step that references this group, we store the layout name
                    // as the previous layout name.
                    if (step.GroupId == _groupId)
                        prevName = step.LayoutName;
                }
                return prevName;
            }
        }
    }

    internal class Scaling
    {
        Dictionary<string, string> _maxGroupSizes;
        List<ScalingStep> _steps;

        internal Scaling()
        {
            _maxGroupSizes = new Dictionary<string, string>();
            _steps = new List<ScalingStep>();
        }

        public void SetGroupMaxSize(string groupId, string layoutName)
        {
            _maxGroupSizes[groupId] = layoutName;
            _dirty = true;
        }

        public void RemoveGroupMaxSize(string groupId)
        {
            if (_maxGroupSizes.ContainsKey(groupId))
                _maxGroupSizes.Remove(groupId);
            _dirty = true;
        }

        public string GetGroupMaxSize(string groupId)
        {
            if (!_maxGroupSizes.ContainsKey(groupId))
                return string.Empty;
            return _maxGroupSizes[groupId];
        }

        public void AddScalingStep(ScalingStep step)
        {
            if (CUIUtility.IsNullOrUndefined(step))
                throw new ArgumentNullException("step must be definined and not null");
            if (string.IsNullOrEmpty(GetGroupMaxSize(step.GroupId)))
                throw new InvalidOperationException("You must set the GroupMaxSize of Group: "
                                       + step.GroupId + " before you add ScalingSteps for it");

            AddScalingStepAtIndex(step, _steps.Count);
        }

        public void AddScalingStepAtIndex(ScalingStep step, int index)
        {
            if (_steps.Contains(step))
                throw new InvalidOperationException("This ScalingInfo already contains this ScaleStep");
            _steps.Insert(index, step);
            step.SetParent(this);
            _dirty = true;
        }

        public void RemoveScalingStep(ScalingStep step)
        {
            _steps.Remove(step);
            step.SetParent(null);
            _dirty = true;
        }

        public List<ScalingStep> Steps
        {
            get 
            {
                return new List<ScalingStep>(_steps);
            }
        }

        public List<ScalingStep> StepsInternal
        {
            get 
            { 
                return _steps; 
            }
        }

        bool _dirty = true;
        internal bool Dirty
        {
            get 
            { 
                return _dirty; 
            }
            set 
            { 
                _dirty = value; 
            }
        }
    }
}
