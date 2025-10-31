using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace AircraftLightsGUI
{
    public class MainForm : Form
    {
        private Panel planePanel;
        private Button emergencyButton;
        private Button exitButton;
        private TextBox infoTextBox;

        // Lists of all the lights
        private List<StatusLight> lights;
        private StatusLight selectedLight;

        // The different light states
        public enum LightStatus
        {
            Off,
            On,
            Fault,
            Emergency
        }

        // Class that defines the properties for each light
        public class StatusLight
        {
            public string ID { get; set; } // Unique ID
            public string DisplayName { get; set; } // Display name
            public PointF Position { get; set; } // position
            public LightStatus Status { get; set; } // state
            public float Radius { get; set; } = 8f; // Size
            public bool IsAisleLight { get; set; } = false;
            public float Height { get; set; } = 30f; // Size (Seperate for isle lights)

            // Detects if a light has been clicked
            public bool Contains(PointF point)
            {
                if (IsAisleLight)
                {
                    RectangleF rect = new RectangleF(Position.X - 3, Position.Y - (Height / 2), 6, Height);
                    return rect.Contains(point);
                }
                else
                {
                    float dx = point.X - Position.X;
                    float dy = point.Y - Position.Y;
                    return (dx * dx + dy * dy) <= (Radius * Radius);
                }
            }

            // Updates the colour of the lights circle
            public Color GetColor() => Status switch
            {
                LightStatus.Off => Color.White,
                LightStatus.On => Color.LimeGreen,
                LightStatus.Fault => Color.Yellow,
                LightStatus.Emergency => Color.Red,
                _ => Color.White
            };
        }

        // Constructor for the form
        public MainForm()
        {
            InitializeComponent();
            InitializeLights();
        }
        // Sets up the form and the controls
        private void InitializeComponent()
        {
            Text = "Aircraft Status Monitor";
            Size = new Size(400, 550);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Plane drawing panel
            planePanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(370, 395),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            planePanel.Paint += PlanePanel_Paint;
            planePanel.MouseClick += PlanePanel_MouseClick;
            Controls.Add(planePanel);
            // Emergency button
            emergencyButton = new Button
            {
                Text = "Emergency",
                Location = new Point(10, 415),
                Size = new Size(100, 40),
                Font = new Font("Arial", 9)
            };
            emergencyButton.Click += EmergencyButton_Click;
            Controls.Add(emergencyButton);

            // Exit button
            exitButton = new Button
            {
                Text = "Exit",
                Location = new Point(10, 465),
                Size = new Size(100, 40)
            };
            exitButton.Click += (s, e) => Close();
            Controls.Add(exitButton);

            // Textbox
            infoTextBox = new TextBox
            {
                Location = new Point(130, 415),
                Size = new Size(250, 90),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.White,
                Text = ""
            };
            Controls.Add(infoTextBox);
        }

        // Creates and positions all of the lights
        private void InitializeLights()
        {
            lights = new List<StatusLight>();

            // Nose section
            lights.AddRange(new[]
            {
                new StatusLight { ID = "co00", DisplayName = "Cockpit Front", Position = new PointF(185, 70) },
                new StatusLight { ID = "co01", DisplayName = "Cockpit Left", Position = new PointF(170, 95) },
                new StatusLight { ID = "co02", DisplayName = "Cockpit Right", Position = new PointF(200, 95) }
            });

            // Passenger seat rows
            float startY = 140;
            int numRows = 4;
            int seatCounter = 0;
            for (int row = 1; row <= numRows; row++)
            {
                float y = startY + (row - 1) * 45;
                lights.Add(new StatusLight { ID = $"se{seatCounter:D2}", DisplayName = $"Seat {row}A", Position = new PointF(165, y) });
                seatCounter++;
                lights.Add(new StatusLight { ID = $"se{seatCounter:D2}B", DisplayName = $"Seat {row}B", Position = new PointF(205, y) });
                seatCounter++;
            }

            // Aisle lights - these are different since they are rectuangular
            lights.AddRange(new[]
            {
                new StatusLight { ID = "ai01", DisplayName = "Aisle Light 1", Position = new PointF(185, 150), IsAisleLight = true, Height = 50f },
                new StatusLight { ID = "ai02", DisplayName = "Aisle Light 2", Position = new PointF(185, 200), IsAisleLight = true, Height = 50f },
                new StatusLight { ID = "ai03", DisplayName = "Aisle Light 3", Position = new PointF(185, 250), IsAisleLight = true, Height = 50f }
            });

            // Tail
            lights.Add(new StatusLight { ID = "ta00", DisplayName = "Left Tail", Position = new PointF(135, 335) });
            lights.Add(new StatusLight { ID = "ta01", DisplayName = "Tail", Position = new PointF(185, 350) });
            lights.Add(new StatusLight { ID = "ta02", DisplayName = "Right Tail", Position = new PointF(235, 335) });

            // Wing tips
            lights.AddRange(new[]
            {
                new StatusLight { ID = "wi00", DisplayName = "Left Wing", Position = new PointF(40, 195) },
                new StatusLight { ID = "wi01", DisplayName = "Right Wing", Position = new PointF(330, 195) }
            });
        }

        private void PlanePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawAircraftOutline(g);
            //DrawSeperationLines(g);
            //DrawAircraftBorder(g);
            DrawLegend(g);
            DrawLights(g);
        }

        // Draws aircraft outline
        private void DrawAircraftOutline(Graphics g)
        {
            using (Pen planePen = new Pen(Color.Black, 2.5f))
            {
                GraphicsPath path = new GraphicsPath();

                // Draws the  aircrafts main body
                path.AddArc(160, 40, 50, 50, 180, 180);
                path.AddLine(210, 65, 218, 120);
                path.AddLine(218, 120, 222, 185);
                path.AddLine(222, 185, 340, 185);
                path.AddLine(340, 185, 337, 205);
                path.AddLine(337, 205, 222, 205);
                path.AddLine(222, 205, 215, 310);
                path.AddLine(215, 310, 200, 355);
                path.AddLine(200, 355, 185, 362);
                path.AddLine(185, 362, 170, 355);
                path.AddLine(170, 355, 155, 310);
                path.AddLine(155, 310, 148, 205);
                path.AddLine(148, 205, 30, 205);
                path.AddLine(30, 205, 33, 185);
                path.AddLine(33, 185, 148, 185);
                path.AddLine(148, 185, 152, 120);
                //path.AddLine(152, 120, 160, 65);
                //path.AddLine(155, 310, 215, 310);
                path.CloseFigure();
                g.DrawPath(planePen, path);

                // Tail 
                g.DrawLine(planePen, 160, 330, 120, 330);
                g.DrawLine(planePen, 120, 330, 123, 342);
                g.DrawLine(planePen, 123, 342, 165, 342);
                g.DrawLine(planePen, 210, 330, 250, 330);
                g.DrawLine(planePen, 250, 330, 247, 342);
                g.DrawLine(planePen, 247, 342, 205, 342);

                g.DrawLine(planePen, 152, 120, 218, 120);
                g.DrawLine(planePen, 155, 310, 215, 310);
                g.DrawLine(planePen, 148, 185, 148, 205);
                g.DrawLine(planePen, 222, 185, 222, 205);
            }
        }

        // Draws the 'key' for the colours on the top right of the GUI
        private void DrawLegend(Graphics g)
        {
            int keyX = 265, keyY = 30;
            using (Font keyFont = new Font("Arial", 10, FontStyle.Bold))
            using (Font labelFont = new Font("Arial", 8))
            {
                g.DrawString("Key:", keyFont, Brushes.Black, keyX, keyY);
                var legendItems = new (Color color, string text)[] {
                    (Color.Red, "Emergency"),
                    (Color.Yellow, "Fault"),
                    (Color.LimeGreen, "On"),
                    (Color.White, "Off")
                };

                for (int i = 0; i < legendItems.Length; i++)
                {
                    var item = legendItems[i];
                    int y = keyY + 25 + (i * 25);
                    g.FillEllipse(new SolidBrush(item.color), keyX, y, 14, 14);
                    g.DrawEllipse(Pens.Black, keyX, y, 14, 14);
                    g.DrawString(item.text, labelFont, Brushes.Black, keyX + 18, y + 1);
                }
            }
        }

        // Draws all the lights
        private void DrawLights(Graphics g)
        {
            foreach (var light in lights)
            {
                if (light.IsAisleLight)
                {
                    RectangleF rect = new RectangleF(light.Position.X - 3, light.Position.Y - (light.Height / 2), 6, light.Height);
                    g.FillRectangle(new SolidBrush(light.GetColor()), rect);
                    using var pen = new Pen(light == selectedLight ? Color.Blue : Color.Black, light == selectedLight ? 3 : 1);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else
                {
                    g.FillEllipse(new SolidBrush(light.GetColor()),
                        light.Position.X - light.Radius, light.Position.Y - light.Radius,
                        light.Radius * 2, light.Radius * 2);
                    using var pen = new Pen(light == selectedLight ? Color.Blue : Color.Black, light == selectedLight ? 3 : 1);
                    g.DrawEllipse(pen, light.Position.X - light.Radius, light.Position.Y - light.Radius,
                        light.Radius * 2, light.Radius * 2);
                }
            }
        }

        // Handles mouse clicks to select lights
        private void PlanePanel_MouseClick(object sender, MouseEventArgs e)
        {
            PointF clickPoint = new PointF(e.X, e.Y);
            selectedLight = lights.FirstOrDefault(l => l.Contains(clickPoint));
            if (selectedLight != null)
                ShowLightContextMenu(selectedLight, e.Location);
            planePanel.Invalidate();
        }

        // Shows the pop-up when a light is clicked
        private void ShowLightContextMenu(StatusLight light, Point location)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add($"{light.DisplayName}").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            void AddItem(string text, LightStatus status)
            {
                var item = new ToolStripMenuItem(text);
                //update below to call Joels method to update light status
                //item.Click += (s, e) => SetLightStatus(light, status);
                menu.Items.Add(item);
            }

            AddItem("Off (White)", LightStatus.Off);
            AddItem("On (Green)", LightStatus.On);
            AddItem("Fault (Yellow)", LightStatus.Fault);
            AddItem("Emergency (Red)", LightStatus.Emergency);

            menu.Show(planePanel, location);
        }

        // Function called to update lights
        private void SetLightStatus(StatusLight light, bool isFault, bool isEmergency, bool isOn)
        {
            if (isFault)
            { light.Status = LightStatus.Fault; }
            else if (isEmergency)
            { light.Status = LightStatus.Emergency; }
            else if (isOn)
            { light.Status = LightStatus.On; }
            else { light.Status = LightStatus.Off; }
            planePanel.Invalidate();
        }

        // The same function but without 'emergency' since not every light has that property
        private void UpdateLightStatus(StatusLight light, bool isFault, bool isOn)
        {
            if (isFault)
            { light.Status = LightStatus.Fault; }
            else if (isOn)
            { light.Status = LightStatus.On; }
            else { light.Status = LightStatus.Off; }
            planePanel.Invalidate();
        }

        // Emergency toggle
        bool IsEmergency = false;
        private void EmergencyButton_Click(object sender, EventArgs e)
        {
            if (IsEmergency == false)
            {
                IsEmergency = true;
                foreach (var light in lights)
                {
                    if (light.ID.StartsWith("se")) {; light.Status = LightStatus.Off; }
                    else if (light.ID.StartsWith("co") || light.ID.StartsWith("ai")) { light.Status = LightStatus.Emergency; }
                    // add in code here which turns on emergency in the lights class
                }
            }
            else
            {
                IsEmergency = false;
                // add in code here which turns off emergency in the lights class
            }
            planePanel.Invalidate();
        }
    }
}
