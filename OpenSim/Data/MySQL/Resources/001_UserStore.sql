BEGIN;

SET FOREIGN_KEY_CHECKS=0;
-- ----------------------------
-- Table structure for agents
-- ----------------------------
CREATE TABLE `agents` (
  `UUID` varchar(36) NOT NULL,
  `sessionID` varchar(36) NOT NULL,
  `secureSessionID` varchar(36) NOT NULL,
  `agentIP` varchar(16) NOT NULL,
  `agentPort` int(11) NOT NULL,
  `agentOnline` tinyint(4) NOT NULL,
  `loginTime` int(11) NOT NULL,
  `logoutTime` int(11) NOT NULL,
  `currentRegion` varchar(36) NOT NULL,
  `currentHandle` bigint(20) unsigned NOT NULL,
  `currentPos` varchar(64) NOT NULL,
  PRIMARY KEY  (`UUID`),
  UNIQUE KEY `session` (`sessionID`),
  UNIQUE KEY `ssession` (`secureSessionID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Create schema avatar_appearance
--

CREATE TABLE `avatarappearance` (
  Owner char(36) NOT NULL,
  Serial int(10) unsigned NOT NULL,
  Visual_Params blob NOT NULL,
  Texture blob NOT NULL,
  Avatar_Height float NOT NULL,
  Body_Item char(36) NOT NULL,
  Body_Asset char(36) NOT NULL,
  Skin_Item char(36) NOT NULL,
  Skin_Asset char(36) NOT NULL,
  Hair_Item char(36) NOT NULL,
  Hair_Asset char(36) NOT NULL,
  Eyes_Item char(36) NOT NULL,
  Eyes_Asset char(36) NOT NULL,
  Shirt_Item char(36) NOT NULL,
  Shirt_Asset char(36) NOT NULL,
  Pants_Item char(36) NOT NULL,
  Pants_Asset char(36) NOT NULL,
  Shoes_Item char(36) NOT NULL,
  Shoes_Asset char(36) NOT NULL,
  Socks_Item char(36) NOT NULL,
  Socks_Asset char(36) NOT NULL,
  Jacket_Item char(36) NOT NULL,
  Jacket_Asset char(36) NOT NULL,
  Gloves_Item char(36) NOT NULL,
  Gloves_Asset char(36) NOT NULL,
  Undershirt_Item char(36) NOT NULL,
  Undershirt_Asset char(36) NOT NULL,
  Underpants_Item char(36) NOT NULL,
  Underpants_Asset char(36) NOT NULL,
  Skirt_Item char(36) NOT NULL,
  Skirt_Asset char(36) NOT NULL,
  PRIMARY KEY  (`Owner`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

SET FOREIGN_KEY_CHECKS=0;
-- ----------------------------
-- Table structure for users
-- ----------------------------
CREATE TABLE `userfriends` (
   `ownerID` VARCHAR(37) NOT NULL,
   `friendID` VARCHAR(37) NOT NULL,
   `friendPerms` INT NOT NULL,
   `datetimestamp` INT NOT NULL,
	 UNIQUE KEY  (`ownerID`, `friendID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
-- ----------------------------
-- Table structure for users
-- ----------------------------
CREATE TABLE `users` (
  `UUID` varchar(36) NOT NULL default '',
  `username` varchar(32) NOT NULL,
  `lastname` varchar(32) NOT NULL,
  `passwordHash` varchar(32) NOT NULL,
  `passwordSalt` varchar(32) NOT NULL,
  `homeRegion` bigint(20) unsigned default NULL,
  `homeLocationX` float default NULL,
  `homeLocationY` float default NULL,
  `homeLocationZ` float default NULL,
  `homeLookAtX` float default NULL,
  `homeLookAtY` float default NULL,
  `homeLookAtZ` float default NULL,
  `created` int(11) NOT NULL,
  `lastLogin` int(11) NOT NULL,
  `userInventoryURI` varchar(255) default NULL,
  `userAssetURI` varchar(255) default NULL,
  `profileCanDoMask` int(10) unsigned default NULL,
  `profileWantDoMask` int(10) unsigned default NULL,
  `profileAboutText` text,
  `profileFirstText` text,
  `profileImage` varchar(36) default NULL,
  `profileFirstImage` varchar(36) default NULL,
  `webLoginKey` varchar(36) default NULL,
  PRIMARY KEY  (`UUID`),
  UNIQUE KEY `usernames` (`username`,`lastname`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Records 
-- ----------------------------
COMMIT;