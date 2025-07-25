﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using C_TestForge.Models.TestCases;
using LiteDB;

namespace C_TestForge.TestCase.Repositories
{
    public class TestCaseRepository : ITestCaseRepository
    {
        private readonly string _dbPath;
        private readonly IMapper _mapper;

        public TestCaseRepository(string dbPath, IMapper mapper)
        {
            _dbPath = dbPath;
            _mapper = mapper;

            // Ensure directory exists
            var directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task<List<Models.TestCases.TestCase>> GetAllAsync()
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var collection = db.GetCollection<Models.TestCases.TestCase>("testcases");
                return await Task.FromResult(collection.FindAll().ToList());
            }
        }

        public async Task<Models.TestCases.TestCase> GetByIdAsync(Guid id)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var collection = db.GetCollection<Models.TestCases.TestCase>("testcases");
                return await Task.FromResult(collection.FindById(new BsonValue(id)));
            }
        }

        public async Task<List<Models.TestCases.TestCase>> GetByFunctionNameAsync(string functionName)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var collection = db.GetCollection<Models.TestCases.TestCase>("testcases");
                return await Task.FromResult(collection
                    .Find(x => x.FunctionName == functionName)
                    .ToList());
            }
        }

        public async Task<Models.TestCases.TestCase> CreateAsync(Models.TestCases.TestCase testCase)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var collection = db.GetCollection<Models.TestCases.TestCase>("testcases");
                collection.Insert(testCase);
                return await Task.FromResult(testCase);
            }
        }

        public async Task<Models.TestCases.TestCase> UpdateAsync(Models.TestCases.TestCase testCase)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var collection = db.GetCollection<Models.TestCases.TestCase>("testcases");
                collection.Update(testCase);
                return await Task.FromResult(testCase);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var collection = db.GetCollection<Models.TestCases.TestCase>("testcases");
                return await Task.FromResult(collection.Delete(new BsonValue(id)));
            }
        }

        public async Task<bool> DeleteAllAsync()
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var collection = db.GetCollection<Models.TestCases.TestCase>("testcases");
                return await Task.FromResult(collection.DeleteAll() > 0);
            }
        }
    }
}
