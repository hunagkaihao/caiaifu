-- WorkPositions (工位表)
CREATE TABLE IF NOT EXISTS `WorkPositions` (
  `Id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `DeviceName` varchar(128) NOT NULL COMMENT '设备名称',
  `SiteName` varchar(128) NOT NULL COMMENT '站点名称',
  `Status` varchar(16) NOT NULL DEFAULT '可用' COMMENT '状态：可用/禁用',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工位表';
