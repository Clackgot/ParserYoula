using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace ParserYoula
{
    class YoulaDataBase
    {
        private string dbFileName;

        public string DbFileName
        {
            get { return dbFileName; }
        }
        private SQLiteConnection connection;

        public SQLiteConnection Connection
        {
            get { return connection; }
        }

        private SQLiteCommand command;

        public SQLiteCommand Command
        {
            get { return command; }
        }

        public YoulaDataBase(string name)
        {
            connection = new SQLiteConnection();
            command = new SQLiteCommand();
            dbFileName = name;
        }

        public void Create()
        {
            bool newDB = false;
            if (!File.Exists(dbFileName))
            {
                SQLiteConnection.CreateFile(dbFileName);
                newDB = true;
            }

            try
            {
                connection = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                connection.Open();
                command.Connection = connection;

                if (newDB)
                {
                    command.CommandText = @"CREATE TABLE products (id	INTEGER NOT NULL UNIQUE, productId	TEXT NOT NULL UNIQUE,ownerId	TEXT NOT NULL UNIQUE,description	TEXT,price	INTEGER,marks INTEGER check(marks >= 0 and marks <= 2),PRIMARY KEY(id AUTOINCREMENT))";
                    command.ExecuteNonQuery();
                    AddTestData();
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("Disconnected");
                Console.WriteLine(ex.Message);
            }
        }

        private void AddProductsData()
        {
            command.CommandText = "INSERT INTO products (productId, ownerId, description, price, marks) VALUES (:productId, :ownerId, :description, :price, :marks)";
            SQLiteTransaction transaction = connection.BeginTransaction();//запускаем транзакцию
            try
            {
                for (int i = 1; i < 101; i++)
                {
                    command.Parameters.AddWithValue("productId", $"product_{i}");
                    command.Parameters.AddWithValue("ownerId", $"owner_{i}");
                    command.Parameters.AddWithValue("description", $"description_{i}");
                    command.Parameters.AddWithValue("price", i+2);
                    command.Parameters.AddWithValue("marks", i*2);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine(e.Message);
                throw;
            }
        }


        private void AddTestData()
        {
            //AddProductsData();
        }

        public List<Product> AddProducts(List<Product> products)
        {
            List<Product> addedProducts = new List<Product>();
            command.CommandText = "INSERT or IGNORE INTO products (productId, ownerId, description, price, marks) VALUES (:productId, :ownerId, :description, :price, :marks)";
            SQLiteTransaction transaction = connection.BeginTransaction();//запускаем транзакцию
            try
            {
                foreach (var product in products)
                {
                    command.Parameters.AddWithValue("productId", product.Id);
                    command.Parameters.AddWithValue("ownerId", product.OwnerId);
                    command.Parameters.AddWithValue("description", product.Description);
                    command.Parameters.AddWithValue("price", product.Price);
                    command.Parameters.AddWithValue("marks", product.MarksCount);
                    
                    bool isAdded = Convert.ToBoolean(command.ExecuteNonQuery());
                    if (isAdded) addedProducts.Add(product);
                }
                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine(e.Message);
                throw;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Найдено новых объявлений: {addedProducts.Count}");
            Console.ForegroundColor = ConsoleColor.Gray;
            return addedProducts;
        }


        public void PrintProducts()
        {
            command.CommandText = "SELECT * FROM products";
            DataTable data = new DataTable();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
            adapter.Fill(data);
            Console.WriteLine($"Прочитано {data.Rows.Count} записей из таблицы products");
            foreach (DataRow row in data.Rows)
            {
                Console.WriteLine($"id = {row.Field<long>("id")} " +
                    $"productId = {row.Field<string>("productId")} " +
                    $"ownerId = {row.Field<string>("ownerId")} " +
                    $"description = {row.Field<string>("description")} " +
                    $"price = {row.Field<long>("price")} " +
                    $"marks = {row.Field<long>("marks")} ");
            }
        }

    }
}
