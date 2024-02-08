using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenDesign
{

    public class EDIElement
    {
        private int min;                    // Minimum length of a valid element
        private int max;                    // Maximum length of a valid element
        private string type;                // Data type {AN, ID, DT, TM, N0, R, Nx}
        private string value;               // Proposed value of the element
        private int valueInt;               // Element value converted to int
        private double valueDouble;         // Element value converted to double
        private DateTime valueDateTime;     // Element value converted to datetime
        private bool valid;                 // True if the element has a value meeting valid criteria
        private bool empty;                 // True it the element has NO value

        // Parameterized constructor 
        public EDIElement(int min, int max, string type, string value)
        {
            SetElement(min, max, type, value);
        }

        // Copy constructor
        public EDIElement(EDIElement element)
        {
            SetElement(element.min, element.max, element.type, element.value);
        }

        // Helper method for initialization
        private void SetElement(int min, int max, string type, string value)
        {
            this.min = min;
            this.max = max;
            this.type = type;
            this.value = value;
            // Determine if value has content
            if (value.Length > 0) this.empty = false;
            else this.empty = true;
            // Determine if value is valid according to base attributes
            if (value.Length < this.min || value.Length > this.max) { valid = false; }
            // Validate an integer
            else if (type == "N0") valid = int.TryParse(value, out valueInt);
            // Validate an implied decimal
            else if (type[0] == 'N' && type.Length == 2) valid = int.TryParse(value, out valueInt);
            // Validate a real number
            else if (type == "R") valid = double.TryParse(value, out valueDouble);
            // Validate a time
            else if (type == "TM")
            {
                string format = "HHmm";
                if (value.Length == 6) format = "HHmmss";
                valid = DateTime.TryParseExact(value, format, new CultureInfo("en-US"), DateTimeStyles.None, out valueDateTime);
            }
            // Validate a date
            else if (type == "DT")
            {
                string format = "yyyyMMdd";
                if (value.Length == 6) format = "yyMMdd";
                valid = DateTime.TryParseExact(value, format, new CultureInfo("en-US"), DateTimeStyles.None, out valueDateTime);
            }
            // Validate text
            else if (type == "ID" || type == "AN") valid = true;
            // Otherwise default to invalid
            else valid = false;
        }

        // Report on validity of the data in the element
        public bool GetValid() { return valid; }

        // Report on presence of data in the element
        public bool GetEmpty() { return empty; }

        // Report the value of the element (if it is valid)
        public string GetValue()
        {
            if (valid)
            {
                if (type[0] == 'N' && type.Length == 2)
                {
                    // Convert the implied decimal field to explicit decimal string
                    int exponent = 0;
                    int.TryParse(type.Substring(1, 1), out exponent);
                    double dValue = valueInt / (Math.Pow(10, exponent));
                    return dValue.ToString();
                }
                else
                    return value;
            }
            else
                return string.Empty;
        }

        // Override the inherited ToString() method
        public override string ToString() { return GetValue(); }
    }

    public class EDISegment : CollectionBase
    {
        private char separator;             // Character reserved as the Element separator
        private string segmentName;         // 2 or 3 character name for the segment
        private bool valid;                 // True if all elements are valid and requirement criteria are met 

        // Parameterized constructor
        public EDISegment(string name, char separator, EDIElement[] elements)
        {
            this.segmentName = name;
            this.separator = separator;
            this.valid = true;
            for (int i = 0; i < elements.Length; i++)
            {
                InnerList.Add(elements[i]);
            }
            // 
            CheckSegmentRules();
        }

        // String constructor
        public EDISegment(string data, char separator)
        {
            int min = 0;                                // Placeholder for element attribute
            int max = 0;                                // Placeholder for element attribute
            string type = "";                           // Placeholder for element attribute

            this.separator = separator;
            this.valid = true;                          // Default to valid
            string[] parts = data.Split(separator);     // Use the Split method to break the string into an array of elements
            this.segmentName = parts[0];                // The first string will be the segment name
            for (int i = 1; i < parts.Length; i++)      // The remaining strings will be individual elements
            {
                // From the parts[0] (BEG) and i (01) determine min, max, type
                GetEleDef(segmentName, i, ref min, ref max, ref type);
                // Create the element
                EDIElement e = new EDIElement(min, max, type, parts[i]);
                // Check element validity
                if (!e.GetValid() & !e.GetEmpty())
                {
                    this.valid = false;
                }
                // Add the element to the segment list
                InnerList.Add(e);
            }
            CheckSegmentRules();
        }

        // Reconstruct the Segment in EDI string format
        public string GetSegment()
        {
            string retVal = segmentName;
            for (int i = 0; i < InnerList.Count; i++)
            {
                retVal += separator;
                retVal += InnerList[i];
            }
            return retVal;
        }

        // Report the name of the segment (e.g. "BEG")
        public string GetSegmentName()
        {
            return segmentName;
        }

        // Report the number of elements in the segment
        public int GetCount()
        {
            return this.Count;
        }

        // Report on the validity of the segment
        public bool IsValid() { return valid; }

        // Allow the use of [] to dereference elements in the segment
        public EDIElement this[int pointIndex]
        {
            get
            {
                if (pointIndex > 0)
                {
                    EDIElement element = (EDIElement)List[pointIndex - 1];
                    return (EDIElement)List[pointIndex - 1];
                }
                else
                {
                    EDIElement element = (EDIElement)List[0];
                    return (EDIElement)List[0];
                }
            }
            set
            {
                List[pointIndex] = value;
            }
        }

        // Retrieve the defining attributes for a specific element (e.g. BEG01)
        private void GetEleDef(string SegID, int EleNo, ref int min, ref int max, ref string type)
        {
            // Use prebuilt ACCESS database for Element definitions and Segment Rules
            string strProvider = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=EDIStandards.mdb";
            // SQL code to retrieve the element definition for EleNo element in segment SegID
            string strSql = "SELECT * FROM EleDef WHERE SegID = '" + SegID + "' and EleNo = " + EleNo.ToString();
            // Establish connection to the database
            OleDbConnection con = new OleDbConnection(strProvider);
            OleDbCommand cmd = new OleDbCommand(strSql, con);
            con.Open();
            OleDbDataReader reader = cmd.ExecuteReader();
            // Read the first (and hopefully only) record defining the element
            if (reader.Read())
            {
                // Retrieve and set the attribites of the element
                int.TryParse(reader["EleMin"].ToString(), out min);
                int.TryParse(reader["EleMax"].ToString(), out max);
                type = reader["EleType"].ToString();
            }
            else // Element definition not available
            {
                // Default the attributes in a fashion that will make the element invalid
                min = 1; max = 0; type = "AN";
            }
            con.Close();
        }

        // Retrieve and apply all element rules to check the validity of the segment
        private void CheckSegmentRules()
        {
            // Use prebuilt ACCESS database for Element definitions and Segment Rules
            string strProvider = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=EDIStandards.mdb";
            // SQL code to retrieve the rules for segment SegID
            string strSql = "SELECT * FROM SegRul WHERE SegID = '" + segmentName + "'";
            // Establish connection to the database
            OleDbConnection con = new OleDbConnection(strProvider);
            OleDbCommand cmd = new OleDbCommand(strSql, con);
            con.Open();
            OleDbDataReader reader = cmd.ExecuteReader();
            // Read ALL records defining a segment rule
            while (reader.Read() && this.valid)
            {
                string rule = reader["Rule"].ToString();
                string type = rule.Substring(0, 1);
                if (type == "M")
                {
                    int index;
                    if (int.TryParse(rule.Substring(1, 2), out index))
                    {
                        if (index > this.Count)
                        {
                            this.valid = false;
                        }
                        if (this[index].GetEmpty())
                        {
                            this.valid = false;
                        }
                    }
                }
                else
                {
                    //MessageBox.Show(type);
                }
            }
        }
    }

    public class EDITransaction : CollectionBase
    {
        private char separator;             // Character reserved to delimit elements
        private char terminator;            // Character reserved to delimit segments
        private string transactionType;     // Code indicating transaction type (e.g. "810", "850". etc.)
        private bool valid;                 // True if all segments are valid and requirement criteria are met

        // Parameterized constructor
        public EDITransaction(string transactionType, char terminator, char separator, EDISegment[] segments)
        {
            this.transactionType = transactionType;
            this.terminator = terminator;
            this.separator = separator;
            for (int i = 0; i < segments.Length; i++)
            {
                InnerList.Add(segments[i]);
            }
            CheckTransactionRules();
        }

        // String constructor
        public EDITransaction(string data, char terminator, char separator)
        {
            this.terminator = terminator;
            this.separator = separator;
            valid = true;                                            // Default the transaction as valid
            string[] parts = data.Split(terminator);                 // Use split to break the transaction into segments
            for (int i = 0; i < parts.Length; i++)
            {
                EDISegment s = new EDISegment(parts[i], separator);  // Create the segment
                if (!s.IsValid()) this.valid = false;                // Update transaction as invalid if segment is invalid
                InnerList.Add(s);                                    // Add the segment to the transaction
            }
        }

        // Data constructor
        public EDITransaction(string data)
        {
            //this.terminator = '~';
            //this.separator = '*';
            // Read the Envelope header to determine the delimiters
            string ISA;
            if (data.Length > 106)
            {
                ISA = data.Substring(0, 106);
                this.terminator = ISA[105];
                this.separator = ISA[103];
                this.valid = true;
                string[] parts = data.Split(terminator);                 // Use split to break the transaction into segments
                for (int i = 0; i < parts.Length; i++)
                {
                    EDISegment s = new EDISegment(parts[i], separator);  // Create the segment
                    if (!s.IsValid()) this.valid = false;                // Update transaction as invalid if segment is invalid
                    InnerList.Add(s);                                    // Add the segment to the transaction
                }
            }
            else
                this.valid = false;                                      // Missing header means invalid transaction
        }

        // Report on validity of the transaction
        public bool IsValid() { return valid; }

        // Retrive a specific element from the transaction (e.g. "BEG" 03)
        public string GetElement(string segID, int eleNo)
        {
            bool found = false;
            string retVal = "N/A";
            for (int i = 0; i < this.Count & !found; i++)
            {
                if (this[i].GetSegmentName() == segID)
                {
                    if (this[i].GetCount() >= eleNo)
                    {
                        retVal = this[i][eleNo].GetValue();
                    }
                    found = true; break;
                }
            }
            return retVal;
        }

        // Enable [] for accessing each segment in the transaction
        public EDISegment this[int pointIndex]
        {
            get
            {
                if (pointIndex > 0)
                {
                    EDISegment element = (EDISegment)List[pointIndex - 1];
                    return (EDISegment)List[pointIndex - 1];
                }
                else
                {
                    EDISegment element = (EDISegment)List[0];
                    return (EDISegment)List[0];
                }
            }
            set
            {
                List[pointIndex] = value;
            }
        }

        // Stub function for applying ANSI Standard rules for transaction set validation
        private void CheckTransactionRules()
        {
            this.valid = true;
        }
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////
    /// </summary>
    public class EDILoop : CollectionBase
    {
        // Constructor extracting sections from a transaction
        public EDILoop(EDITransaction txn, string start, string end)
        {
            int txnIndex = 0;
            // Skip the segments prior to the start of the looped sections
            while (txnIndex < txn.Count && txn[txnIndex].GetSegmentName() != start)
            {
                txnIndex++;
            }

            // Use only the segment before the designated end
            while (txnIndex < txn.Count && txn[txnIndex].GetSegmentName() != end)
            {
                // Create the section and add the starting segment
                EDISection section = new EDISection();
                section.AddSegment(txn[txnIndex]);
                txnIndex++;
                // Add all segments until the starting segment is repeated
                while (txnIndex < txn.Count && txn[txnIndex].GetSegmentName() != start && txn[txnIndex].GetSegmentName() != end)
                {
                    section.AddSegment(txn[txnIndex]);
                    txnIndex++;
                }
                // Add the section to the loop
                this.InnerList.Add(section);
            }
        }

        // Retrieve the element from the specific (numbered) section in the loop
        public string GetElement(string segName, int eleNo, int instance)
        {
            return this[instance].GetElement(segName, eleNo);
        }

        // Enable [] for accessing each section in the loop
        public EDISection this[int pointIndex]
        {
            get
            {
                if (pointIndex > 0)
                {
                    EDISection element = (EDISection)List[pointIndex - 1];
                    return (EDISection)List[pointIndex - 1];
                }
                else
                {
                    EDISection element = (EDISection)List[0];
                    return (EDISection)List[0];
                }
            }
            set
            {
                List[pointIndex] = value;
            }
        }

    }

    public class EDISection : CollectionBase
    {
        // Retrive a specific element from the section (e.g. "BEG" 03)
        public string GetElement(string segID, int eleNo)
        {
            bool found = false;
            string retVal = "N/A";
            for (int i = 0; i < this.Count & !found; i++)
            {
                if (this[i].GetSegmentName() == segID)
                {
                    if (this[i].GetCount() >= eleNo)
                    {
                        retVal = this[i][eleNo].GetValue();
                    }
                    found = true; break;
                }
            }
            return retVal;
        }

        // Add a segment to the section
        public void AddSegment(EDISegment segment)
        {
            this.InnerList.Add(segment);
        }

        // Enable [] for accessing each segment in the section
        public EDISegment this[int pointIndex]
        {
            get
            {
                if (pointIndex > 0)
                {
                    EDISegment element = (EDISegment)List[pointIndex - 1];
                    return (EDISegment)List[pointIndex - 1];
                }
                else
                {
                    EDISegment element = (EDISegment)List[0];
                    return (EDISegment)List[0];
                }
            }
            set
            {
                List[pointIndex] = value;
            }
        }


        /* public class EDIElement
         {

             private int min;
             private int max;
             private string type;
             private string value;
             private int valueInt;
             private float valueFloat;
             private DateTime valueDateTime;
             private bool valid;
             private bool empty;




             public EDIElement(int min, int max, string type, string value)
             {
                 SetElement(min, max, type, value);
             }
             //param constructor
             public EDIElement(EDIElement element)
             {
                 SetElement(element.min, element.max, element.type, element.value);  
             }
             //copy constructor
             private void SetElement(int min, int max, string type, string value)
             {
                 this.min = 0;
                 if (min > 0) this.min = min;
                 if (max > min) this.max = max;
                 else this.max = this.min;
                 this.type = type;
                 this.value = value;
                 if (value.Length > 0)
                     this.empty = false;
                 else this.empty = true;
                 if (value.Length > this.min || value.Length > this.max) { this.valid = false; }

                 else if (type == "NO") valid = int.TryParse(value, out valueInt);
                 else if (type[0] == 'N' && type.Length == 2) valid = float.TryParse(value, out valueFloat);
                 else if (type == "DT") valid = DateTime.TryParse(value, out valueDateTime);
                 else if (type == "TM") valid = DateTime.TryParse(value, out valueDateTime);
                 else if (type == "ID" || type == "AN") valid = true;
                 else
                     valid = true;
             }
             // report validity of the element
             public bool GetValid() { return valid; }
             // report the value of the element (if it is valid)
             public string getValue()
             {

                 if (GetValid()){
                     return this.value;
                 }
                 return "";

             }
             public override string ToString()
             {
                 return getValue(); 
             }
         }


         public class EDISegment : CollectionBase 
         {

             private string name;
             private char separator;  

             public EDISegment(string name, char separator, EDIElement [ ] elements)
             {
                 this.name = name;
                 this.separator = separator;
                 for (int i = 0; i < elements.Length; i++)
                 {
                     InnerList.Add(elements[i]);
                 }
             }


             public EDISegment(string data, char separator)
             {
                 this.separator=separator;
                 string[] parts = data.Split(separator);
                 this.name = parts[0];
                 for (int i = 0; i < parts.Length; i++)
                 {
                     int min = 1;
                     int max = 32;
                     string type = "AN";
                     EDIElement e = new EDIElement(min, max, type, parts[i]);
                     InnerList.Add(e);
                 }
             }

             public string GetSegment()
             {
                 string retVal = name;
                 for (int i=0; i < InnerList.Count; i++)
                 {
                     retVal += separator;
                     retVal += (EDIElement)InnerList[i];
                 }
                 return retVal;
             }
         }*/
    }


    }


