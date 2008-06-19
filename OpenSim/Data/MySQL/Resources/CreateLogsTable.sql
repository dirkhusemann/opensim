CREATE TABLE `logs` (
  `logID` int(10) unsigned NOT NULL auto_increment,
  `target` varchar(36) default NULL,
  `server` varchar(64) default NULL,
  `method` varchar(64) default NULL,
  `arguments` varchar(255) default NULL,
  `priority` int(11) default NULL,
  `message` text,
  PRIMARY KEY  (`logID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8
