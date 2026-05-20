using System;
using System.Drawing;
using System.Windows.Forms;
using MaxCustomControls;

namespace EasyMaxWeightedNormal
{
    internal sealed class WeightedNormalOptionsForm : MaxForm
    {
        private readonly EasyWeightedNormalModifier modifier;
        private readonly CheckBox areaWeightCheckBox;
        private readonly CheckBox angleWeightCheckBox;
        private readonly CheckBox smoothingGroupsCheckBox;
        private bool syncing;

        public WeightedNormalOptionsForm(EasyWeightedNormalModifier modifier)
        {
            this.modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));

            Text = PluginConstants.ClassName + " Options";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = false;
            ClientSize = new Size(260, 132);

            areaWeightCheckBox = CreateCheckBox("Area weight", 12, 12);
            angleWeightCheckBox = CreateCheckBox("Angle weight", 12, 40);
            smoothingGroupsCheckBox = CreateCheckBox("Respect smoothing groups", 12, 68);

            var resetButton = new Button
            {
                Text = "Reset",
                Location = new Point(12, 100),
                Size = new Size(72, 24)
            };
            resetButton.Click += ResetButton_Click;

            var closeButton = new Button
            {
                Text = "Close",
                Location = new Point(176, 100),
                Size = new Size(72, 24)
            };
            closeButton.Click += (sender, args) => Close();

            Controls.Add(areaWeightCheckBox);
            Controls.Add(angleWeightCheckBox);
            Controls.Add(smoothingGroupsCheckBox);
            Controls.Add(resetButton);
            Controls.Add(closeButton);

            SyncFromModifier();
        }

        public void SyncFromModifier()
        {
            syncing = true;
            try
            {
                WeightedNormalSettings settings = modifier.CurrentSettings;
                areaWeightCheckBox.Checked = settings.UseAreaWeight;
                angleWeightCheckBox.Checked = settings.UseAngleWeight;
                smoothingGroupsCheckBox.Checked = settings.RespectSmoothingGroups;
            }
            finally
            {
                syncing = false;
            }
        }

        private CheckBox CreateCheckBox(string text, int x, int y)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(220, 22),
                AutoSize = false
            };
            checkBox.CheckedChanged += CheckBox_CheckedChanged;
            return checkBox;
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (syncing)
            {
                return;
            }

            modifier.UpdateSettings(ReadSettings());
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            WeightedNormalSettings defaults = WeightedNormalSettings.Default;

            syncing = true;
            try
            {
                areaWeightCheckBox.Checked = defaults.UseAreaWeight;
                angleWeightCheckBox.Checked = defaults.UseAngleWeight;
                smoothingGroupsCheckBox.Checked = defaults.RespectSmoothingGroups;
            }
            finally
            {
                syncing = false;
            }

            modifier.UpdateSettings(defaults);
        }

        private WeightedNormalSettings ReadSettings()
        {
            return new WeightedNormalSettings
            {
                UseAreaWeight = areaWeightCheckBox.Checked,
                UseAngleWeight = angleWeightCheckBox.Checked,
                RespectSmoothingGroups = smoothingGroupsCheckBox.Checked
            };
        }
    }
}
