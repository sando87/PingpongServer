using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace postgreDBServer
{
    class Utils
    {
        static public bool isSetBit(uint val, int idx) { return (val & (1<<idx)) > 0 ? true : false; }
        static public bool isSetBit64(UInt64 val, int idx) { return (val & (1UL << idx)) > 0 ? true : false; }
        static public void setBit(ref uint val, int idx) { val |= (uint)(1 << idx); }
        static public void setBit64(ref UInt64 val, int idx) { val |= ((UInt64)1 << idx); }
        static public void clearBit(ref uint val, int idx) { val &= ~(uint)(1 << idx); }
        static public string getVersion()
        {
            //1. Assembly.GetExecutingAssembly().FullName의 값은
            //'ApplicationName, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
            //와 같다.
            string strVersionText = Assembly.GetExecutingAssembly().FullName
                    .Split(',')[1]
                    .Trim()
                    .Split('=')[1];

            string[] version = strVersionText.Split('.');

            //2. Version Text의 세번째 값(Build Number)은 2000년 1월 1일부터
            //Build된 날짜까지의 총 일(Days) 수 이다.
            int intDays = Convert.ToInt32(version[2]);
            DateTime refDate = new DateTime(2000, 1, 1);
            DateTime dtBuildDate = refDate.AddDays(intDays);

            //3. Verion Text의 네번째 값(Revision NUmber)은 자정으로부터 Build된
            //시간까지의 지나간 초(Second) 값 이다.
            int intSeconds = Convert.ToInt32(version[3]);
            intSeconds = intSeconds * 2;
            dtBuildDate = dtBuildDate.AddSeconds(intSeconds);


            //4. 시차조정
            DaylightTime daylingTime = TimeZone.CurrentTimeZone
                    .GetDaylightChanges(dtBuildDate.Year);
            if (TimeZone.IsDaylightSavingTime(dtBuildDate, daylingTime))
                dtBuildDate = dtBuildDate.Add(daylingTime.Delta);

            return dtBuildDate.ToString("yyMMdd");
        }
        static public bool LoadFile(DataGridView _view, out string _name)
        {
            _name = "";
            _view.RowHeadersVisible = false;
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "csv(*.csv)|*.csv";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _name = dlg.FileName;
                        StreamReader sr = new StreamReader(dlg.FileName);

                        _view.Columns.Clear();
                        string firstLine = sr.ReadLine();
                        string[] tops = firstLine.Split(',');
                        for (int i = 0; i < tops.Length; ++i)
                        {
                            _view.Columns.Add(tops[i], tops[i]);
                            _view.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                        }


                        _view.Rows.Clear();
                        while (sr.Peek() != -1)
                        {
                            string line = sr.ReadLine();
                            string[] cols = line.Split(',');
                            _view.Rows.Add(cols);
                        }
                        return true;
                    }
                    catch { MessageBox.Show("파일이 열려있습니다."); }
                }
            }
            return false;
        }
        static public bool LoadFile(ListView _view)
        {
            _view.View = View.Details;
            _view.FullRowSelect = true;
            _view.GridLines = true;
            _view.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            _view.Items.Clear();
            _view.Columns.Clear();
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "csv(*.csv)|*.csv";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StreamReader sr = new StreamReader(dlg.FileName);

                        string firstLine = sr.ReadLine();
                        string[] tops = firstLine.Split(',');
                        for (int i = 0; i < tops.Length; ++i)
                            _view.Columns.Add(tops[i]);

                        while (sr.Peek() != -1)
                        {
                            string line = sr.ReadLine();
                            string[] cols = line.Split(',');
                            _view.Items.Add(new ListViewItem(cols));
                        }
                        return true;
                    }
                    catch { MessageBox.Show("파일이 열려있습니다."); }
                }
            }
            return false;
        }
        static public string[] LoadFile(out string _name)
        {
            _name = " ";
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "csv(*.csv)|*.csv";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StreamReader sr = new StreamReader(dlg.FileName);
                        _name = dlg.FileName.Split('\\').Last();

                        List<string> vec = new List<string>();
                        while (sr.Peek() != -1)
                        {
                            string line = sr.ReadLine();
                            vec.Add(line);
                        }
                        return vec.ToArray();
                    }
                    catch { MessageBox.Show("파일이 열려있습니다."); }
                }
            }
            return new string[0];
        }
        static public byte[] LoadBinaryFile(out string _name)
        {
            _name = " ";
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _name = dlg.FileName.Split('\\').Last();
                    return File.ReadAllBytes(@dlg.FileName);
                }
            }
            return new byte[0];
        }
        static public string SaveFile(ListView _view, string _filename = null)
        {
            if (_view.Items.Count == 0)
                return "저장 실패";

            string[] lines = new string[_view.Items.Count];
            for (int i = 0; i < _view.Items.Count; ++i)
                lines[i] = String.Join( ",", readRow(_view, i) );
                
            if(_filename != null)
            {
                string[] path = _filename.Split('\\');
                if (path.Length > 1)
                {
                    CreateFolder(path[0]);
                }

                using (StreamWriter outputFile = new StreamWriter(@_filename))
                {
                    foreach (string line in lines)
                    {
                        outputFile.WriteLine(line);
                    }
                }
                return _filename;
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "csv(*.csv)|*.csv";
            saveFileDialog1.Title = DateTime.Now.ToString("");
            saveFileDialog1.ShowDialog();
            _filename = saveFileDialog1.FileName;

            if (_filename != "")
            {
                //File.WriteAllText(saveFileDialog1.FileName, txt);
                File.WriteAllLines(saveFileDialog1.FileName, lines);
                return _filename;
            }
            return "저장 실패";
        }
        static public string SaveFile(DataGridView _view, string _filename = null)
        {
            if (_view.Rows.Count == 0)
                return " ";

            string[] lines = new string[_view.Rows.Count];
            for (int i = 0; i < _view.Rows.Count; ++i)
                lines[i] = String.Join(",", readRow(_view, i));

            if (_filename != null)
            {
                string[] path = _filename.Split('\\');
                if (path.Length > 1)
                {
                    CreateFolder(path[0]);
                }

                using (StreamWriter outputFile = new StreamWriter(@_filename))
                {
                    foreach (string line in lines)
                        outputFile.WriteLine(line);
                }
                return _filename;
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "csv(*.csv)|*.csv";
            saveFileDialog1.Title = DateTime.Now.ToString("");
            saveFileDialog1.ShowDialog();
            _filename = saveFileDialog1.FileName;

            if (_filename != "")
            {
                //File.WriteAllText(saveFileDialog1.FileName, txt);
                File.WriteAllLines(saveFileDialog1.FileName, lines);
                return _filename;
            }
            return " ";
        }
        static public string SaveFile(string[] _data, string _filename = null)
        {
            if (_data.Length == 0)
                return "";

            if (_filename != null)
            {
                string[] path = _filename.Split('\\');
                if(path.Length > 1)
                {
                    CreateFolder(path[0]);
                }

                using (StreamWriter outputFile = new StreamWriter(@_filename))
                {
                    foreach (string line in _data)
                    {
                        outputFile.WriteLine(line);
                    }
                }
                return _filename;
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "csv(*.csv)|*.csv";
            saveFileDialog1.Title = DateTime.Now.ToString("HHmmss");
            saveFileDialog1.ShowDialog();
            _filename = saveFileDialog1.FileName.Split('\\').Last();

            if (_filename != "")
            {
                //File.WriteAllText(saveFileDialog1.FileName, txt);
                File.WriteAllLines(saveFileDialog1.FileName, _data);
                return _filename;
            }
            return "";
        }
        static public void CreateFolder(string _dirName)
        {
            string sDirPath = Application.StartupPath + "\\" + _dirName;
            DirectoryInfo di = new DirectoryInfo(sDirPath);
            if (di.Exists == false)
            {
                di.Create();
            }
        }
        static public void InitListView(ListView _lv, string[] _columnList)
        {
            _lv.View = View.Details;
            _lv.FullRowSelect = true;
            _lv.GridLines = true;
            _lv.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _lv.Columns.Clear();
            for (int i = 0; i < _columnList.Length; ++i)
                _lv.Columns.Add(_columnList[i]);
        }
        static public string ToHexString(byte[] _buf)
        {
            string ret = "";
            int del = 8;
            int lines = _buf.Length / del;
            string[] hexs = BitConverter.ToString(_buf).Split('-');
            string[] strLines = new string[lines];
            int line = 0;
            for(line=0; line < lines; line++)
            {
                strLines[line] = String.Format("{0:D3}: ", line + 1) + String.Join(" ", hexs, line * del, del);
            }
            ret = String.Join("\r\n", strLines);

            int remains = _buf.Length % del;
            if(remains > 0)
            {
                string remStr = String.Format("{0:D3}: ", line+1) + String.Join(" ", hexs, lines * del, remains);
                ret += "\r\n" + remStr;
            }

            return ret;
        }
        static public string[] ToHexList(byte[] _buf)
        {
            return BitConverter.ToString(_buf).Split('-');
        }
        static public void ExcuteEXE(string _name, string _path = null)
        {
            if (_path == null)
                _path = Application.StartupPath;
            string fullname = _path + "\\" +_name;
            Process.Start(fullname);
        }
        static public int ToMsg(object _msg, string[] _vals, int _idx) //string타입의 values 값들을 그에 맞는 구조체 값으로 변환
        {
            int idx = _idx;
            FieldInfo[] fields = _msg.GetType().GetFields();
            foreach (var field in fields) //필드값들의 루프를 돔
            {
                object val = new object();
                if (field.FieldType.IsArray) //필드가 배열일 경우
                {
                    System.Collections.IList mm = (System.Collections.IList)field.GetValue(_msg);
                    for (int i = 0; i < mm.Count; ++i)
                    {
                        if (mm[i].GetType().Namespace == "System") //배열필드의 i번째 값이 기본형일 경우
                        {
                            val = stringToOBj(mm[i].GetType().Name, _vals[idx++]); //기본형일 경우 최종 데이터 파싱
                            mm[i] = val;
                        }
                        else //배열필드의 i번째 기본형이 아닐경우
                        {
                            object stObj = mm[i];
                            idx = ToMsg(stObj, _vals, idx);  //기본형 단위가 나올때까지 재귀함수
                            mm[i] = stObj;
                        }
                    }
                }
                else  //현재 필드가 배열이 아닐경우
                {
                    if (field.FieldType.Namespace == "System" || field.FieldType.BaseType.Name == "Enum") //현재 필드 기본형일 경우
                    {
                        if(field.FieldType.BaseType.Name == "Enum")
                        {
                            field.SetValue(_msg, Enum.Parse(field.FieldType, _vals[idx++]));
                        }
                        else
                        {
                            val = stringToOBj(field.FieldType.Name, _vals[idx++]); //기본형일 경우 최종 데이터 파싱
                            field.SetValue(_msg, val);
                        }
                    }
                    else  //현재 필드가 기본형이 아닐경우
                    {
                        object stObj = field.GetValue(_msg);
                        idx = ToMsg(stObj, _vals, idx); //기본형 단위가 나올때까지 재귀함수
                        field.SetValue(_msg, stObj);
                    }
                }
            }
            return idx;
        }
        static public string ToStringCSV(stHeader _msg)
        {
            string[] vals = Utils.ToValues(_msg);
            int headCount = 11;
            string[] headVals = vals.Skip(vals.Length - headCount).Take(headCount).ToArray();
            string ret = String.Join(",", headVals) + ",";
            string[] bodyVals = vals.Take(vals.Length - headCount).ToArray();
            ret += String.Join(",", bodyVals);
            return ret;
        }
        static public string[] ToValues(object _msg, string _type = null)
        {
            List<string> tmp = new List<string>();
            FieldInfo[] fields = _msg.GetType().GetFields();
            foreach (var field in fields)
            {
                if (_type != null && _type != field.DeclaringType.Name)
                    continue;

                if (field.FieldType.IsArray)
                {
                    System.Collections.IList mm = (System.Collections.IList)field.GetValue(_msg);
                    for (int i = 0; i < mm.Count; ++i)
                    {
                        if (mm[i].GetType().Namespace == "System")
                            tmp.Add(mm[i].ToString());
                        else
                        {
                            string[] subList = ToValues(mm[i]);
                            tmp.AddRange(subList);
                        }
                    }
                }
                else
                {
                    if (field.FieldType.Namespace == "System" || field.FieldType.BaseType.Name == "Enum") //기본형일때
                    {
                        if(field.FieldType.BaseType.Name == "Enum")
                        {
                            var enumObj = field.GetValue(_msg) as Enum;
                            tmp.Add(enumObj.ToString("X"));
                        }
                        else
                            tmp.Add(field.GetValue(_msg).ToString());
                        
                    }
                    else
                    {
                        string[] subList = ToValues(field.GetValue(_msg));
                        tmp.AddRange(subList);
                    }
                }
            }

            return tmp.ToArray();
        }
        static public string[] ToNames(object _msg, string _type = null)
        {
            List<string> tmp = new List<string>();
            var type = _msg.GetType();
            FieldInfo[] fields = _msg.GetType().GetFields();
            foreach (var field in fields)
            {
                if (_type != null && _type != field.DeclaringType.Name)
                    continue;

                int numberA = 1;
                int numberB = 1;
                if (field.FieldType.IsArray) //배열일때
                {
                    System.Collections.IList mm = (System.Collections.IList)field.GetValue(_msg);
                    for (int i = 0; i < mm.Count; ++i)
                    {
                        if (mm[i].GetType().Namespace == "System") //기본형일때
                        {
                            tmp.Add(field.Name + numberA.ToString());
                            numberA++;
                        }
                        else //구조체나 클래스일때
                        {
                            string[] subList = ToNames(mm[i]);
                            for (int j = 0; j < subList.Length; ++j)
                                subList[j] += numberB.ToString();
                            tmp.AddRange(subList);
                            numberB++;
                        }
                    }
                    continue;
                }
                else
                {
                    if (field.FieldType.Namespace == "System" || field.FieldType.BaseType.Name == "Enum") //기본형일때
                    {
                        tmp.Add(field.Name);
                    }
                    else
                    {
                        string[] subList = ToNames(field.GetValue(_msg));
                        tmp.AddRange(subList);
                        continue;
                    }
                }

                
            }

            return tmp.ToArray();
        }
        static public string[] readRow(DataGridView _gridview, int rowIndex)
        {
            if (rowIndex >= _gridview.Rows.Count)
                return new string[0];

            var row = _gridview.Rows[rowIndex].Cells;
            List<string> vec = new List<string>();
            //한 줄을 vec변수에 넣는 for문. Cell을 string으로 변환 (1개씩)
            for (int i = 0; i < row.Count; i++)
            {
                string item = row[i].Value.ToString();
                vec.Add(item);
            }

            return vec.ToArray();//모든 데이터가 입력된 후 배열로 변환하여 리턴
        }
        static public string[] readRow(ListView _lv, int rowIndex)
        {
            if (rowIndex >= _lv.Items.Count)
                return new string[0];

            int count = _lv.Items[rowIndex].SubItems.Count;
            string[] ret = new string[count];
            for (int i = 0; i < count; i++)
                ret[i] = _lv.Items[rowIndex].SubItems[i].Text;

            return ret;
        }
        static public object stringToOBj(string _typeName, string _data)
        {
            object obj = new object();
            switch(_typeName)
            {
                case "UInt16": obj = UInt16.Parse(_data); break;
                case "UInt32": obj = UInt32.Parse(_data); break;
                case "UInt64": obj = UInt64.Parse(_data); break;
                case "Single" : obj = Single.Parse(_data); break;
                case "double": obj = double.Parse(_data); break;
                case "Byte"  : obj = Byte  .Parse(_data); break;
                case "Int16" : obj = Int16 .Parse(_data); break;
                case "Int32" : obj = Int32 .Parse(_data); break;
                case "Int64" : obj = Int64 .Parse(_data); break;
                case "byte"  : obj = byte  .Parse(_data); break;
                case "short" : obj = short .Parse(_data); break;
                case "int"   : obj = int   .Parse(_data); break;
                case "long"  : obj = long  .Parse(_data); break;
            }
            return obj;
        }
        static public void setDefaultControls(Control _ctrl)
        {
            foreach(Control ctrl in _ctrl.Controls)
            {
                switch(ctrl.GetType().Name)
                {
                    case "ComboBox": ((ComboBox)ctrl).SelectedIndex = 0; break;
                    case "TextBox": ((TextBox)ctrl).Text = "0"; break;
                    case "CheckBox": ((CheckBox)ctrl).Checked = false; break;
                    case "GroupBox": setDefaultControls(ctrl); break;
                    case "Panel": setDefaultControls(ctrl); break;
                }
            }
        }
        static public byte CalcCRC(byte[] _image)
        {
            byte crc = 0;
            int cnt = _image.Length;
            for (int i = 0; i < cnt; ++i)
            {
                crc ^= _image[i];
            }
            return crc;
        }
        static public string ConvertHex64(UInt64 _val)
        {
            UInt32 lsb = (UInt32)_val;
            UInt32 msb = (UInt32)(_val >> 32);
            return msb.ToString("X8") + "_" + lsb.ToString("X8");
        }
        static public byte[] Serialize(object obj)
        {
            var buffer = new byte[Marshal.SizeOf(obj)];
            var gch = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var pBuffer = gch.AddrOfPinnedObject();
            Marshal.StructureToPtr(obj, pBuffer, false);
            gch.Free();

            return buffer;
        }
        static public void Deserialize<T>(ref T obj, byte[] data, int size = 0, int off = 0)
        {
            byte[] buf = data;
            if (size > 0)
            {
                byte[] tmp = new byte[size];
                Array.Copy(data, off, tmp, 0, size);
                buf = tmp;
            }
            var gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
            Marshal.PtrToStructure(gch.AddrOfPinnedObject(), obj);
            gch.Free();
        }
    }

}
