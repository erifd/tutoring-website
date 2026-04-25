const low    = require('lowdb');
const FileSync = require('lowdb/adapters/FileSync');
const path   = require('path');
const fs     = require('fs');

const dataDir = path.join(__dirname, 'data');
fs.mkdirSync(dataDir, { recursive: true });

const file    = path.join(dataDir, 'db.json');
const adapter = new FileSync(file);
const db      = low(adapter);

// Default schema
db.defaults({ users: [] }).write();

module.exports = db;
