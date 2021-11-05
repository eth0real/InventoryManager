using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace InventoryManager
{
    class Program
    {
        private const string FILEPATH_ErrorLog = "error.log";
        private const string FILEPATH_Inventory = "Inventory.json";
        private const string DIRECTORYPATH_InventoryHistory = "History";
        private const string FILEPATH_OrderHistory = "OutboundLog.json";
        private const string strCommands = "[I] Input Orders, [V] View Inventory, [CTRL+S] To Save When Inputting";

        static List<Product> _inventory;
        static List<Product> Inventory
        {
            get
            {
                if (_inventory == null)
                {
                    string fileData = File.ReadAllText(FILEPATH_Inventory);
                    _inventory = JsonConvert.DeserializeObject<List<Product>>(fileData);
                    _inventory.Sort((x, y) => Comparer<string>.Default.Compare(x.Name, y.Name));
                }

                return _inventory;
            }
        }

        class OutboundLog
        {
            List<Outbound> _entries;
            public List<Outbound> Entries
            {
                get
                {
                    if (_entries == null)
                    {
                        if (File.Exists(FILEPATH_OrderHistory))
                        {
                            string fileData = File.ReadAllText(FILEPATH_OrderHistory);
                            _entries = JsonConvert.DeserializeObject<List<Outbound>>(fileData);
                            _entries.Sort((x, y) => Comparer<DateTime>.Default.Compare(x.Date, y.Date));
                        }
                        else
                        {
                            _entries = new List<Outbound>();
                        }
                    }

                    return _entries;
                }
            }

            public void Add(Outbound outbound)
            {
                outbound.Date = DateTime.Now;
                Entries.Add(outbound);
            }

            public void Save()
            {
                string data = JsonConvert.SerializeObject(Entries, Formatting.Indented);
                File.WriteAllText(FILEPATH_OrderHistory, data);
            }
        }


        static void UpdateInventory(Outbound outbound)
        {
            foreach (var order in outbound.Orders)
            {
                var product = Inventory.Find(p => p.Name == order.Key);
                product.Inventory -= order.Value;
                if (product.Inventory < 0)
                {
                    System.Console.WriteLine("negative inventory set to zero");
                    product.Inventory = 0;
                }
            }

            // send orders to order log
            OutboundLog outboundLog = new OutboundLog();
            outboundLog.Add(outbound);
            outboundLog.Save();

            var entriesFromLast30Days = outboundLog.Entries.FindAll(e => e.Date > DateTime.Now.AddDays(-30));
            var spanDays = Math.Ceiling(DateTime.Now.Subtract(entriesFromLast30Days[0].Date).TotalDays);
            Dictionary<string, int> productsSales = new Dictionary<string, int>();
            foreach (var entry in entriesFromLast30Days)
            {
                foreach (var order in entry.Orders)
                {
                    if (!productsSales.ContainsKey(order.Key))
                        productsSales.Add(order.Key, 0);

                    productsSales[order.Key] += order.Value;
                }
            }

            foreach (var product in Inventory)
            {
                if (productsSales.ContainsKey(product.Name))
                {
                    product.AverageOrdersPerDay = productsSales[product.Name] / spanDays;
                }
            }

            // save inventory
            string data = JsonConvert.SerializeObject(Inventory, Formatting.Indented);
            File.WriteAllText(FILEPATH_Inventory, data);

            // backup inventory file
            string path = $"{DIRECTORYPATH_InventoryHistory}/{DateTime.Now:yyyyMMdd}.json";
            File.WriteAllText(path, data);

        }

        //static void UpdateIn

        static void GetEstimatedDaysOfRemainingInventory()
        {
            // get total orders per product last month

        }

        private enum Mode
        {
            None,
            InputOrders,
            ViewHistory//,
            //ViewInventory
        }

        private enum SubMode
        {
            None,
            EnteringQuantity,
            SelectingProduct,
            CommitOrders
        }

        static void Main(string[] args)
        {
            try
            {
                if (!Directory.Exists(DIRECTORYPATH_InventoryHistory))
                    Directory.CreateDirectory(DIRECTORYPATH_InventoryHistory);

                Mode currentMode = Mode.None;
                SubMode subMode = SubMode.None;

                ConsoleKeyInfo cki;
                // Prevent example from ending if CTL+C is pressed.
                Console.TreatControlCAsInput = true;

                Console.WriteLine("###############################################################");
                Console.WriteLine("###################### Inventory Manager ######################");
                Console.WriteLine("###############################################################");
                Console.WriteLine();
                Console.WriteLine(strCommands);
                Console.WriteLine("Press the Escape (Esc) key to quit: \n");

                string currentString = "";
                int skip = 0;
                Product currentProduct = null;
                Outbound outbound = null;

                do
                {
                    cki = Console.ReadKey();
                    //Console.Write(" --- You pressed ");
                    if ((cki.Modifiers & ConsoleModifiers.Alt) != 0) Console.Write("ALT+");
                    if ((cki.Modifiers & ConsoleModifiers.Shift) != 0) Console.Write("SHIFT+");
                    if ((cki.Modifiers & ConsoleModifiers.Control) != 0) Console.Write("CTL+");
                    //Console.WriteLine(cki.Key.ToString());

                    switch (currentMode)
                    {
                        case Mode.None:
                            ClearCurrentConsoleLine();
                            switch (cki.Key.ToString().ToUpper())
                            {
                                case "I":
                                    currentMode = Mode.InputOrders;
                                    Console.WriteLine("Begin entering orders:");
                                    currentProduct = ShowProductOptions(currentString, skip);
                                    subMode = SubMode.SelectingProduct;
                                    outbound = new Outbound();
                                    break;
                                case "V":
                                    //currentMode = Mode.ViewInventory;
                                    Console.WriteLine("\nInventory: \n");
                                    Console.WriteLine("| Product | Inventory | Avg Per Day | Days Remaining |");
                                    Console.WriteLine("______________________________________________________");
                                    foreach (var product in Inventory)
                                    {
                                        Console.WriteLine(String.Format("|{0,20}|{1,5}|{2,5}|{3,5}|", product, product.Inventory, product.AverageOrdersPerDay, product.EstimatedDaysOfRemainingInventory));
                                        //Console.WriteLine($"{product} : {product.Inventory} | {product.AverageOrdersPerDay} | {product.EstimatedDaysOfRemainingInventory}");
                                    }
                                    Console.WriteLine($"\n{strCommands}");
                                    currentMode = Mode.None;
                                    break;
                                //case "H":
                                //    currentMode = Mode.ViewHistory;
                                //    Console.WriteLine("View History");
                                //    break;
                                default:
                                    Console.WriteLine($"\"{cki.Key}\" is not an option.");
                                    break;
                            }
                            break;

                        case Mode.InputOrders:

                            if ((cki.Modifiers & ConsoleModifiers.Control) != 0 && cki.Key.ToString().ToLower() == "s")
                            {
                                ClearCurrentConsoleLine();
                                Console.WriteLine($"Commit Changes to {outbound.Orders.Count} Products?");
                                subMode = SubMode.CommitOrders;
                            }

                            switch (subMode)
                            {
                                case SubMode.SelectingProduct:
                                    switch (cki.Key)
                                    {
                                        case ConsoleKey.Enter:
                                            // confirm
                                            if (currentProduct != null)
                                            {
                                                currentString = "";
                                                ClearCurrentConsoleLine();
                                                Console.Write($"{currentProduct} : {currentString}");
                                                subMode = SubMode.EnteringQuantity;
                                            }
                                            break;
                                        case ConsoleKey.Backspace:
                                        case ConsoleKey.Delete:
                                            currentString = currentString == "" ? "" : currentString.Substring(0, currentString.Length - 1);
                                            currentProduct = ShowProductOptions(currentString, skip);
                                            break;
                                        case ConsoleKey.Tab:
                                            skip++;
                                            currentProduct = ShowProductOptions(currentString, skip);
                                            break;
                                        default:
                                            currentString += cki.KeyChar;
                                            skip = 0;
                                            currentProduct = ShowProductOptions(currentString, skip);
                                            break;
                                    }
                                    break;
                                case SubMode.EnteringQuantity:
                                    switch (cki.Key)
                                    {
                                        case ConsoleKey.Enter:
                                            // confirm

                                            int sold;
                                            if (Int32.TryParse(currentString, out sold))
                                            {
                                                currentString = "";
                                                Console.WriteLine();
                                                if (outbound.Orders.ContainsKey(currentProduct.Name))
                                                {
                                                    Console.WriteLine($"Warning: Already added orders for this {currentProduct}.");
                                                    outbound.Orders[currentProduct.Name] += sold;
                                                }
                                                else
                                                {
                                                    outbound.Orders.Add(currentProduct.Name, sold);
                                                }
                                                subMode = SubMode.SelectingProduct;
                                                currentProduct = ShowProductOptions(currentString, skip);
                                            }
                                            break;
                                        case ConsoleKey.Backspace:
                                        case ConsoleKey.Delete:
                                            currentString = currentString == "" ? "" : currentString.Substring(0, currentString.Length - 1);
                                            ClearCurrentConsoleLine();
                                            Console.Write($"{currentProduct} : {currentString}");
                                            break;
                                        default:
                                            if (char.IsDigit(cki.KeyChar))
                                            {
                                                currentString += cki.KeyChar;
                                                ClearCurrentConsoleLine();
                                                Console.Write($"{currentProduct} : {currentString}");
                                            }
                                            break;
                                    }
                                    break;
                                case SubMode.CommitOrders:
                                    switch (cki.Key.ToString().ToUpper())
                                    {
                                        case "Y":
                                            ClearCurrentConsoleLine();
                                            currentMode = Mode.None;
                                            subMode = SubMode.None;
                                            UpdateInventory(outbound);
                                            Console.WriteLine($"\n{strCommands}");
                                            break;

                                        case "N":
                                            ClearCurrentConsoleLine();
                                            Console.WriteLine($"\n{strCommands}");
                                            currentMode = Mode.None;
                                            subMode = SubMode.None;
                                            break;

                                        default:
                                            ClearCurrentConsoleLine();
                                            Console.Write("Press [Y] to Confirm, [N] to Cancel.");
                                            break;
                                    }
                                    break;
                                default:

                                    break;

                            }
                            break;
                        default:
                            break;
                    }
                } while (cki.Key != ConsoleKey.Escape);

                // verfiy all names and id's are unique

                // read log, generate estimated monthly sales

                //commands
                // display current inventory with number of days remaining of inventory
                // input inventory changes
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        static void LogError(Exception e)
        {
            File.AppendAllText(FILEPATH_ErrorLog, e.ToString());
        }

        static Product ShowProductOptions(string inputString, int skip = 0)
        {
            var applicableProducts = Inventory.FindAll(p => p.Name.ToLower().StartsWith(inputString.ToLower()));
            Product currentProduct = null;

            if (applicableProducts.Count > 0)
            {
                currentProduct = applicableProducts[skip % applicableProducts.Count];
                ClearCurrentConsoleLine();
                Console.Write($"{(!String.IsNullOrEmpty(inputString) ? inputString + " " : "")}[{currentProduct}]");
            }
            else
            {
                currentProduct = null;
                ClearCurrentConsoleLine();
                Console.Write($"{inputString} [NO PRODUCT MATCHES]");
            }

            return currentProduct;
        }
    }
}
