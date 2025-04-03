using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace bessonov_students
{
    public partial class Form1 : Form
    {
        private List<Dictionary<string, object>> students = new List<Dictionary<string, object>>();
        private string dataFilePath = "students.json";
        private bool isDataChanged = false;

        private DataGridView dataGridView1 = new DataGridView();
        private Button btnAdd = new Button();
        private Button btnEdit = new Button();
        private Button btnDelete = new Button();
        private Button btnExportJson = new Button();
        private Button btnImportJson = new Button();
        private Button btnExportCsv = new Button();
        private Button btnImportCsv = new Button();
        private TextBox txtSearch = new TextBox();
        private ComboBox cmbCourseFilter = new ComboBox();
        private ComboBox cmbGroupFilter = new ComboBox();
        private Label lblStatus = new Label();

        public Form1()
        {
            InitializeCustomComponents();
            LoadData();
            RefreshDataGridView();
            UpdateFilterLists();
        }

        private void InitializeCustomComponents()
        {
            // Основные настройки формы
            this.Text = "Учет студентов";
            this.ClientSize = new System.Drawing.Size(1000, 550);
            this.FormClosing += Form1_FormClosing;

            // Главный контейнер с вертикальным расположением
            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.RowCount = 4;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Панель действий
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Панель фильтров
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Панель экспорта/импорта
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Статус бар

            // 1. Панель основных действий
            var actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.Height = 40;

            btnAdd.Text = "Добавить";
            btnAdd.Width = 100;
            btnEdit.Text = "Редактировать";
            btnEdit.Width = 120;
            btnDelete.Text = "Удалить";
            btnDelete.Width = 100;

            txtSearch.Text = "Поиск по фамилии";
            txtSearch.ForeColor = System.Drawing.Color.Gray;
            txtSearch.Width = 200;
            txtSearch.GotFocus += RemoveSearchPlaceholder;
            txtSearch.LostFocus += ShowSearchPlaceholder;

            actionPanel.Controls.Add(btnAdd);
            actionPanel.Controls.Add(btnEdit);
            actionPanel.Controls.Add(btnDelete);
            actionPanel.Controls.Add(txtSearch);

            // 2. Панель фильтров
            var filterPanel = new FlowLayoutPanel();
            filterPanel.Dock = DockStyle.Fill;
            filterPanel.Height = 40;

            cmbCourseFilter.Width = 120;
            cmbCourseFilter.Items.Add("Все курсы");
            cmbGroupFilter.Width = 120;
            cmbGroupFilter.Items.Add("Все группы");

            filterPanel.Controls.Add(new Label { Text = "Курс:", AutoSize = true, Margin = new Padding(0, 8, 0, 0) });
            filterPanel.Controls.Add(cmbCourseFilter);
            filterPanel.Controls.Add(new Label { Text = "Группа:", AutoSize = true, Margin = new Padding(10, 8, 0, 0) });
            filterPanel.Controls.Add(cmbGroupFilter);

            // 3. Панель экспорта/импорта
            var exportPanel = new FlowLayoutPanel();
            exportPanel.Dock = DockStyle.Fill;
            exportPanel.Height = 40;

            btnExportJson.Text = "Экспорт JSON";
            btnExportJson.Width = 120;
            btnImportJson.Text = "Импорт JSON";
            btnImportJson.Width = 120;
            btnExportCsv.Text = "Экспорт CSV";
            btnExportCsv.Width = 120;
            btnImportCsv.Text = "Импорт CSV";
            btnImportCsv.Width = 120;

            exportPanel.Controls.Add(btnExportJson);
            exportPanel.Controls.Add(btnImportJson);
            exportPanel.Controls.Add(btnExportCsv);
            exportPanel.Controls.Add(btnImportCsv);

            // Настройка DataGridView
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true; // Запрещаем прямое редактирование в ячейках
            dataGridView1.CellBeginEdit += (s, e) => e.Cancel = true; // Блокируем начало редактирования
            InitializeDataGridViewColumns();

            // Строка состояния
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Text = "Всего студентов: 0";
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Добавление панелей на главный контейнер
            mainPanel.Controls.Add(actionPanel, 0, 0);
            mainPanel.Controls.Add(filterPanel, 0, 1);
            mainPanel.Controls.Add(exportPanel, 0, 2);
            mainPanel.Controls.Add(dataGridView1, 0, 3);
            mainPanel.Controls.Add(lblStatus, 0, 4);

            // Подписка на события
            btnAdd.Click += btnAdd_Click;
            btnEdit.Click += btnEdit_Click;
            btnDelete.Click += btnDelete_Click;
            btnExportJson.Click += btnExportJson_Click;
            btnImportJson.Click += btnImportJson_Click;
            btnExportCsv.Click += btnExportCsv_Click;
            btnImportCsv.Click += btnImportCsv_Click;
            txtSearch.TextChanged += txtSearch_TextChanged;
            cmbCourseFilter.SelectedIndexChanged += ApplyFilters;
            cmbGroupFilter.SelectedIndexChanged += ApplyFilters;

            this.Controls.Add(mainPanel);
        }

        private void UpdateFilterLists()
        {
            var courses = students.Select(s => s["Course"].ToString()).Distinct().OrderBy(c => c).ToList();
            var groups = students.Select(s => s["Group"].ToString()).Distinct().OrderBy(g => g).ToList();

            cmbCourseFilter.Items.Clear();
            cmbCourseFilter.Items.Add("Все курсы");
            cmbCourseFilter.Items.AddRange(courses.ToArray());
            cmbCourseFilter.SelectedIndex = 0;

            cmbGroupFilter.Items.Clear();
            cmbGroupFilter.Items.Add("Все группы");
            cmbGroupFilter.Items.AddRange(groups.ToArray());
            cmbGroupFilter.SelectedIndex = 0;
        }

        private void ApplyFilters(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text == "Поиск по фамилии" ? "" : txtSearch.Text.ToLower();
            string courseFilter = cmbCourseFilter.SelectedIndex <= 0 ? null : cmbCourseFilter.SelectedItem.ToString();
            string groupFilter = cmbGroupFilter.SelectedIndex <= 0 ? null : cmbGroupFilter.SelectedItem.ToString();

            var filtered = students.Where(s =>
                (string.IsNullOrEmpty(searchText) || ((string)s["LastName"]).ToLower().Contains(searchText)) &&
                (string.IsNullOrEmpty(courseFilter) || s["Course"].ToString() == courseFilter) &&
                (string.IsNullOrEmpty(groupFilter) || s["Group"].ToString() == groupFilter)
            ).ToList();

            RefreshDataGridView(filtered);
        }

        private void RemoveSearchPlaceholder(object sender, EventArgs e)
        {
            if (txtSearch.Text == "Поиск по фамилии")
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void ShowSearchPlaceholder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Поиск по фамилии";
                txtSearch.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private void InitializeDataGridViewColumns()
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("LastName", "Фамилия");
            dataGridView1.Columns.Add("FirstName", "Имя");
            dataGridView1.Columns.Add("MiddleName", "Отчество");
            dataGridView1.Columns.Add("Course", "Курс");
            dataGridView1.Columns.Add("Group", "Группа");
            dataGridView1.Columns.Add("BirthDate", "Дата рождения");
            dataGridView1.Columns.Add("Email", "Email");
        }

        private void LoadData()
        {
            if (File.Exists(dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(dataFilePath);
                    students = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();
                    isDataChanged = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(students, Formatting.Indented);
                File.WriteAllText(dataFilePath, json);
                isDataChanged = false;
                UpdateFilterLists();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshDataGridView(List<Dictionary<string, object>> data = null)
        {
            dataGridView1.Rows.Clear();
            var displayData = data ?? students;

            foreach (var student in displayData)
            {
                dataGridView1.Rows.Add(
                    student["LastName"],
                    student["FirstName"],
                    student["MiddleName"],
                    student["Course"],
                    student["Group"],
                    ((DateTime)student["BirthDate"]).ToString("dd.MM.yyyy"),
                    student["Email"]
                );
            }
            lblStatus.Text = $"Всего студентов: {displayData.Count}";
        }

        private string ValidateStudent(Dictionary<string, object> student)
        {
            if (string.IsNullOrWhiteSpace((string)student["LastName"]))
                return "Фамилия не может быть пустой";
            if (string.IsNullOrWhiteSpace((string)student["FirstName"]))
                return "Имя не может быть пустым";
            if (string.IsNullOrWhiteSpace((string)student["MiddleName"]))
                return "Отчество не может быть пустым";
            if (string.IsNullOrWhiteSpace((string)student["Group"]))
                return "Группа не может быть пустой";

            string email = (string)student["Email"];
            if (string.IsNullOrWhiteSpace(email))
                return "Email не может быть пустым";
            if (email.IndexOf('@') < 3)
                return "Email должен содержать минимум 3 символа до @";

            DateTime birthDate = (DateTime)student["BirthDate"];
            if (birthDate < new DateTime(1992, 1, 1))
                return "Дата рождения не может быть раньше 01.01.1992";
            if (birthDate > DateTime.Now)
                return "Дата рождения не может быть в будущем";

            return null;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (var form = new AddStudentForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string validationError = ValidateStudent(form.Student);
                    if (validationError == null)
                    {
                        students.Add(form.Student);
                        isDataChanged = true;
                        RefreshDataGridView();
                    }
                    else
                    {
                        MessageBox.Show(validationError, "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите студента для редактирования", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int index = dataGridView1.SelectedRows[0].Index;
            using (var form = new EditStudentForm(students[index]))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string validationError = ValidateStudent(form.Student);
                    if (validationError == null)
                    {
                        students[index] = form.Student;
                        isDataChanged = true;
                        RefreshDataGridView();
                    }
                    else
                    {
                        MessageBox.Show(validationError, "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите студента для удаления", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("Удалить выбранного студента?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int index = dataGridView1.SelectedRows[0].Index;
                students.RemoveAt(index);
                isDataChanged = true;
                RefreshDataGridView();
            }
        }

        private void btnExportJson_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "JSON файлы (*.json)|*.json";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = JsonConvert.SerializeObject(students, Formatting.Indented);
                        File.WriteAllText(saveDialog.FileName, json);
                        MessageBox.Show("Данные успешно экспортированы в JSON", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnImportJson_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "JSON файлы (*.json)|*.json";
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(openDialog.FileName);
                        var imported = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                        if (imported != null)
                        {
                            students = imported;
                            isDataChanged = true;
                            RefreshDataGridView();
                            UpdateFilterLists();
                            MessageBox.Show("Данные успешно импортированы из JSON", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV файлы (*.csv)|*.csv";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var writer = new StreamWriter(saveDialog.FileName))
                        {
                            writer.WriteLine("LastName,FirstName,MiddleName,Course,Group,BirthDate,Email");
                            foreach (var student in students)
                            {
                                writer.WriteLine(
                                    $"\"{student["LastName"]}\"," +
                                    $"\"{student["FirstName"]}\"," +
                                    $"\"{student["MiddleName"]}\"," +
                                    $"{Convert.ToInt32(student["Course"])}," +
                                    $"\"{student["Group"]}\"," +
                                    $"\"{((DateTime)student["BirthDate"]).ToString("dd.MM.yyyy")}\"," +
                                    $"\"{student["Email"]}\"");
                            }
                        }
                        MessageBox.Show("Данные успешно экспортированы в CSV", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnImportCsv_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "CSV файлы (*.csv)|*.csv";
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var imported = new List<Dictionary<string, object>>();
                        using (var reader = new StreamReader(openDialog.FileName))
                        {
                            reader.ReadLine();

                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                var values = line.Split(',');

                                if (values.Length == 7)
                                {
                                    imported.Add(new Dictionary<string, object>
                                    {
                                        {"LastName", values[0].Trim('"')},
                                        {"FirstName", values[1].Trim('"')},
                                        {"MiddleName", values[2].Trim('"')},
                                        {"Course", Convert.ToInt32(values[3])},
                                        {"Group", values[4].Trim('"')},
                                        {"BirthDate", DateTime.ParseExact(values[5].Trim('"'), "dd.MM.yyyy", null)},
                                        {"Email", values[6].Trim('"')}
                                    });
                                }
                            }
                        }

                        students = imported;
                        isDataChanged = true;
                        RefreshDataGridView();
                        UpdateFilterLists();
                        MessageBox.Show("Данные успешно импортированы из CSV", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters(sender, e);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isDataChanged)
            {
                var result = MessageBox.Show("Сохранить изменения перед выходом?", "Подтверждение",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveData();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
    }

    public class AddStudentForm : Form
    {
        public Dictionary<string, object> Student { get; private set; }

        private TextBox txtLastName = new TextBox();
        private TextBox txtFirstName = new TextBox();
        private TextBox txtMiddleName = new TextBox();
        private NumericUpDown numCourse = new NumericUpDown();
        private TextBox txtGroup = new TextBox();
        private DateTimePicker dtpBirthDate = new DateTimePicker();
        private TextBox txtEmail = new TextBox();

        public AddStudentForm()
        {
            InitializeForm();
            Student = new Dictionary<string, object>
            {
                {"LastName", ""},
                {"FirstName", ""},
                {"MiddleName", ""},
                {"Course", 1},
                {"Group", ""},
                {"BirthDate", DateTime.Now},
                {"Email", ""}
            };
        }

        private void InitializeForm()
        {
            this.Text = "Добавление студента";
            this.ClientSize = new System.Drawing.Size(350, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var tableLayout = new TableLayoutPanel();
            tableLayout.Dock = DockStyle.Fill;
            tableLayout.ColumnCount = 2;
            tableLayout.RowCount = 8;

            tableLayout.Controls.Add(new Label { Text = "Фамилия:" }, 0, 0);
            tableLayout.Controls.Add(txtLastName, 1, 0);
            tableLayout.Controls.Add(new Label { Text = "Имя:" }, 0, 1);
            tableLayout.Controls.Add(txtFirstName, 1, 1);
            tableLayout.Controls.Add(new Label { Text = "Отчество:" }, 0, 2);
            tableLayout.Controls.Add(txtMiddleName, 1, 2);
            tableLayout.Controls.Add(new Label { Text = "Курс:" }, 0, 3);
            tableLayout.Controls.Add(numCourse, 1, 3);
            tableLayout.Controls.Add(new Label { Text = "Группа:" }, 0, 4);
            tableLayout.Controls.Add(txtGroup, 1, 4);
            tableLayout.Controls.Add(new Label { Text = "Дата рождения:" }, 0, 5);
            tableLayout.Controls.Add(dtpBirthDate, 1, 5);
            tableLayout.Controls.Add(new Label { Text = "Email:" }, 0, 6);
            tableLayout.Controls.Add(txtEmail, 1, 6);

            numCourse.Minimum = 1;
            numCourse.Maximum = 6;
            dtpBirthDate.MaxDate = DateTime.Now;
            dtpBirthDate.MinDate = new DateTime(1992, 1, 1);
            dtpBirthDate.Format = DateTimePickerFormat.Short;

            var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom };
            var btnSave = new Button { Text = "Сохранить", DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel };

            btnSave.Click += (s, e) =>
            {
                Student["LastName"] = txtLastName.Text;
                Student["FirstName"] = txtFirstName.Text;
                Student["MiddleName"] = txtMiddleName.Text;
                Student["Course"] = (int)numCourse.Value;
                Student["Group"] = txtGroup.Text;
                Student["BirthDate"] = dtpBirthDate.Value;
                Student["Email"] = txtEmail.Text;
            };

            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);

            this.Controls.Add(tableLayout);
            this.Controls.Add(buttonPanel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }
    }

    public class EditStudentForm : Form
    {
        public Dictionary<string, object> Student { get; private set; }

        private TextBox txtLastName = new TextBox();
        private TextBox txtFirstName = new TextBox();
        private TextBox txtMiddleName = new TextBox();
        private NumericUpDown numCourse = new NumericUpDown();
        private TextBox txtGroup = new TextBox();
        private DateTimePicker dtpBirthDate = new DateTimePicker();
        private TextBox txtEmail = new TextBox();

        public EditStudentForm(Dictionary<string, object> student)
        {
            InitializeForm();
            Student = new Dictionary<string, object>(student);

            txtLastName.Text = (string)student["LastName"];
            txtFirstName.Text = (string)student["FirstName"];
            txtMiddleName.Text = (string)student["MiddleName"];
            numCourse.Value = Convert.ToInt32(student["Course"]);
            txtGroup.Text = (string)student["Group"];
            dtpBirthDate.Value = (DateTime)student["BirthDate"];
            txtEmail.Text = (string)student["Email"];
        }

        private void InitializeForm()
        {
            this.Text = "Редактирование студента";
            this.ClientSize = new System.Drawing.Size(350, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var tableLayout = new TableLayoutPanel();
            tableLayout.Dock = DockStyle.Fill;
            tableLayout.ColumnCount = 2;
            tableLayout.RowCount = 8;

            tableLayout.Controls.Add(new Label { Text = "Фамилия:" }, 0, 0);
            tableLayout.Controls.Add(txtLastName, 1, 0);
            tableLayout.Controls.Add(new Label { Text = "Имя:" }, 0, 1);
            tableLayout.Controls.Add(txtFirstName, 1, 1);
            tableLayout.Controls.Add(new Label { Text = "Отчество:" }, 0, 2);
            tableLayout.Controls.Add(txtMiddleName, 1, 2);
            tableLayout.Controls.Add(new Label { Text = "Курс:" }, 0, 3);
            tableLayout.Controls.Add(numCourse, 1, 3);
            tableLayout.Controls.Add(new Label { Text = "Группа:" }, 0, 4);
            tableLayout.Controls.Add(txtGroup, 1, 4);
            tableLayout.Controls.Add(new Label { Text = "Дата рождения:" }, 0, 5);
            tableLayout.Controls.Add(dtpBirthDate, 1, 5);
            tableLayout.Controls.Add(new Label { Text = "Email:" }, 0, 6);
            tableLayout.Controls.Add(txtEmail, 1, 6);

            numCourse.Minimum = 1;
            numCourse.Maximum = 6;
            dtpBirthDate.MaxDate = DateTime.Now;
            dtpBirthDate.MinDate = new DateTime(1992, 1, 1);
            dtpBirthDate.Format = DateTimePickerFormat.Short;

            var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom };
            var btnSave = new Button { Text = "Сохранить", DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel };

            btnSave.Click += (s, e) =>
            {
                Student["LastName"] = txtLastName.Text;
                Student["FirstName"] = txtFirstName.Text;
                Student["MiddleName"] = txtMiddleName.Text;
                Student["Course"] = (int)numCourse.Value;
                Student["Group"] = txtGroup.Text;
                Student["BirthDate"] = dtpBirthDate.Value;
                Student["Email"] = txtEmail.Text;
            };

            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);

            this.Controls.Add(tableLayout);
            this.Controls.Add(buttonPanel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }
    }
}